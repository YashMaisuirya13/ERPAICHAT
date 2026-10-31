using System.Security.Claims;
using System.Text;
using AIAgentService.AI;
using AIAgentService.Services;
using AIAgentService.Tools.DTOs;
using AIAgentService.Tools.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IERPTools _erpTools;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IAIService aiService,
        IERPTools erpTools,
        IChatHistoryService chatHistoryService,
        ILogger<AIController> logger)
    {
        _aiService = aiService;
        _erpTools = erpTools;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
        var conversations = await _chatHistoryService.GetConversationsAsync(userId);
        return Ok(conversations);
    }

    [HttpGet("conversations/{guid:guid}")]
    public async Task<IActionResult> GetConversation(Guid guid)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
        var conversation = await _chatHistoryService.GetConversationAsync(guid, userId);
        if (conversation == null)
        {
            return NotFound(new { message = $"Conversation '{guid}' not found." });
        }
        return Ok(conversation);
    }

    [HttpDelete("conversations/{guid:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid guid)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
        bool deleted = await _chatHistoryService.DeleteConversationAsync(guid, userId);
        if (!deleted)
        {
            return NotFound(new { message = $"Conversation '{guid}' not found." });
        }
        return Ok(new { success = true, message = "Conversation deleted." });
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            return BadRequest(new
            {
                message = "Prompt is required."
            });
        }

        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;

        // Persist or retrieve chat conversation in database
        var conversation = await _chatHistoryService.GetOrCreateConversationAsync(request.ConversationGuid, request.Prompt, userId);

        // Retrieve prior conversation history for stateful multi-turn context (last 10 turns)
        var recentHistory = await _chatHistoryService.GetRecentMessagesAsync(conversation.Id, 10);

        // Save User prompt to database
        await _chatHistoryService.SaveMessageAsync(conversation.Id, "user", request.Prompt, null, null);

        _logger.LogInformation("[AIController] Received Chat Prompt: {Prompt} (Session: {SessionGuid}, Provider: {Provider}, PriorHistory: {Count})", 
            request.Prompt, conversation.ConversationGuid, _aiService.ProviderName, recentHistory.Count);

        try
        {
            var result = await _aiService.ExecuteChatAgentAsync(request.Prompt, recentHistory);

            // Save AI response to database
            await _chatHistoryService.SaveMessageAsync(conversation.Id, "ai", result.Response, result.ToolCalled, result.ToolResult);

            return Ok(new
            {
                success = true,
                conversationGuid = conversation.ConversationGuid,
                source = _aiService.ProviderName,
                data = new
                {
                    response = result.Response,
                    toolCalled = result.ToolCalled ?? "none",
                    result = result.ToolResult
                },
                response = result.Response
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[AIController] AI Provider '{Provider}' call failed ({Message}). Falling back to local ERP tools execution.", 
                _aiService.ProviderName, ex.Message);

            var (explanation, dataResult, toolCalled) = await ExecuteLocalERPToolsAsync(request.Prompt);

            // Save Fallback AI response to database
            await _chatHistoryService.SaveMessageAsync(conversation.Id, "ai", explanation, toolCalled, dataResult);

            return Ok(new
            {
                success = true,
                conversationGuid = conversation.ConversationGuid,
                source = "Local-ERP-Tools-Fallback",
                data = new
                {
                    response = explanation,
                    toolCalled,
                    result = dataResult,
                    diagnosticNotice = $"AI Provider ({_aiService.ProviderName}) Notice: {ex.Message}"
                },
                response = explanation
            });
        }
    }

    [HttpPost("chat/stream")]
    public async Task StreamChat([FromBody] ChatRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            var errJson = System.Text.Json.JsonSerializer.Serialize(new { type = "error", message = "Prompt is required." });
            await Response.WriteAsync($"data: {errJson}\n\n");
            await Response.Body.FlushAsync();
            return;
        }

        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
        var conversation = await _chatHistoryService.GetOrCreateConversationAsync(request.ConversationGuid, request.Prompt, userId);
        var recentHistory = await _chatHistoryService.GetRecentMessagesAsync(conversation.Id, 10);
        await _chatHistoryService.SaveMessageAsync(conversation.Id, "user", request.Prompt, null, null);

        var startJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "start",
            conversationGuid = conversation.ConversationGuid,
            source = _aiService.ProviderName
        });
        await Response.WriteAsync($"data: {startJson}\n\n");
        await Response.Body.FlushAsync();

        string finalResponse = "";
        string? toolCalled = null;
        object? toolResult = null;
        string sourceName = _aiService.ProviderName;

        try
        {
            var result = await _aiService.ExecuteChatAgentAsync(request.Prompt, recentHistory);
            finalResponse = result.Response;
            toolCalled = result.ToolCalled;
            toolResult = result.ToolResult;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[AIController] AI Provider call failed for stream: {Message}. Falling back to local tools.", ex.Message);
            var (fallbackExplanation, fallbackData, fallbackTool) = await ExecuteLocalERPToolsAsync(request.Prompt);
            finalResponse = fallbackExplanation;
            toolCalled = fallbackTool;
            toolResult = fallbackData;
            sourceName = "Local-ERP-Tools-Fallback";
        }

        if (!string.IsNullOrEmpty(toolCalled) && toolCalled != "none")
        {
            var toolJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "tool",
                toolCalled,
                result = toolResult
            });
            await Response.WriteAsync($"data: {toolJson}\n\n");
            await Response.Body.FlushAsync();
        }

        // Stream text in words / tokens with short delay for smooth real-time typing UX
        var words = finalResponse.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            var chunk = (i == 0 ? "" : " ") + words[i];
            var chunkJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "content",
                content = chunk
            });
            await Response.WriteAsync($"data: {chunkJson}\n\n");
            await Response.Body.FlushAsync();
            await Task.Delay(20);
        }

        await _chatHistoryService.SaveMessageAsync(conversation.Id, "ai", finalResponse, toolCalled, toolResult);

        var doneJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "done",
            conversationGuid = conversation.ConversationGuid,
            source = sourceName,
            fullText = finalResponse
        });
        await Response.WriteAsync($"data: {doneJson}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task<(string Explanation, object DataResult, string ToolCalled)> ExecuteLocalERPToolsAsync(string userPrompt)
    {
        var prompt = userPrompt.ToLowerInvariant();
        string toolCalled = "none";
        object dataResult = new object();
        string explanation = "";

        if (prompt.Contains("laptop stock") || prompt.Contains("check laptop"))
        {
            toolCalled = "get_product_inventory & get_low_stock_products";
            var products = await _erpTools.GetAllProductsAsync();
            var laptop = products.FirstOrDefault(p => p.Name.ToLower().Contains("laptop"));
            if (laptop != null)
            {
                var inv = await _erpTools.GetProductInventoryAsync(laptop.Id);
                var available = inv.Sum(i => i.Quantity);
                var whDetails = string.Join(", ", inv.Select(x => $"{x.WarehouseCode}: {x.Quantity} units"));
                explanation = $"### 💻 Laptop Stock Status\n\n- **Product:** {laptop.Name}\n- **SKU:** `{laptop.SKU}`\n- **Total Available:** **{available} units**\n- **Safety Threshold:** {laptop.MinimumStock} units\n- **Warehouse Locations:** {whDetails}\n- **Status:** {(available <= laptop.MinimumStock ? "⚠️ **Low Stock**" : "✅ **Healthy**")}";
                dataResult = new { laptop, available, inventory = inv };
            }
            else
            {
                var lowStock = await _erpTools.GetLowStockProductsAsync();
                explanation = $"### 📦 Product Stock\n\nNo laptop models found in catalog. Checked overall inventory and found {lowStock.Count} low stock item(s).";
                dataResult = lowStock;
            }
        }
        else if (prompt.Contains("prepare a po") || prompt.Contains("find suppliers and prepare") || prompt.Contains("create po") || prompt.Contains("po for 20"))
        {
            toolCalled = "get_suppliers() -> create_purchase_order() -> submit_purchase_order_for_approval()";
            var suppliers = await _erpTools.GetSuppliersAsync();
            if (suppliers.Count == 0)
            {
                explanation = "### ❌ Purchase Order Notice\n\nSearched supplier registry, but no active suppliers are currently available.";
                dataResult = new { suppliers };
            }
            else
            {
                var supplier = suppliers.First();
                var products = await _erpTools.GetAllProductsAsync();
                var laptop = products.FirstOrDefault(p => p.Name.ToLower().Contains("laptop")) ?? products.FirstOrDefault();
                Guid productGuid = laptop?.ProductGuid ?? Guid.NewGuid();
                decimal unitPrice = laptop?.UnitPrice ?? 999.00m;

                var items = new List<PurchaseOrderItemResultDto>
                {
                    new PurchaseOrderItemResultDto
                    {
                        ProductGuid = productGuid,
                        Quantity = 20,
                        UnitPrice = unitPrice
                    }
                };

                var draftPo = await _erpTools.CreateDraftPurchaseOrderAsync(supplier.SupplierGuid, items);
                if (draftPo != null)
                {
                    var submittedPo = await _erpTools.SubmitPurchaseOrderForApprovalAsync(draftPo.PurchaseOrderGuid);
                    string poCode = draftPo.PurchaseOrderGuid.ToString().Substring(0, 8).ToUpper();
                    explanation = $"### ✅ Purchase Order Created & Submitted\n\n- **PO Number:** `PO-{poCode}`\n- **Product:** {laptop?.Name ?? "ThinkPad"}\n- **Quantity:** **20 units**\n- **Supplier:** {supplier.Name}\n- **Unit Price:** ${unitPrice:F2}\n- **Total Amount:** **${(unitPrice * 20):F2}**\n- **Status:** 🟡 **Submitted** (Pending Manager Approval)";
                    dataResult = submittedPo ?? draftPo;
                }
                else
                {
                    explanation = $"### ❌ Draft Failed\n\nFound supplier {supplier.Name}, but could not create draft PO in database.";
                    dataResult = suppliers;
                }
            }
        }
        else if (prompt.Contains("low stock") || prompt.Contains("low in stock"))
        {
            toolCalled = "get_low_stock_products";
            var lowStock = await _erpTools.GetLowStockProductsAsync();
            dataResult = lowStock;
            if (lowStock.Count == 0)
            {
                explanation = "### 🟢 All Stock Levels Healthy\n\nAll inventory items are currently above their minimum safety reorder thresholds.";
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("### ⚠️ Low Stock Inventory Alert");
                sb.AppendLine();
                sb.AppendLine($"Identified **{lowStock.Count} product(s)** below safety reorder threshold:");
                sb.AppendLine();
                sb.AppendLine("| Product Name | SKU | Available Qty | Warehouse | Safety Min | Status |");
                sb.AppendLine("| :--- | :--- | :---: | :--- | :---: | :--- |");
                foreach (var i in lowStock)
                {
                    sb.AppendLine($"| {i.ProductName} | `{i.SKU}` | **{i.Quantity}** | {i.WarehouseCode} | {i.MinimumStock} | ⚠️ Low Stock |");
                }
                sb.AppendLine();
                sb.AppendLine("💡 **Action Recommended:** Prepare a purchase order to replenish items below minimum thresholds.");
                explanation = sb.ToString().Trim();
            }
        }
        else if (prompt.Contains("supplier") || prompt.Contains("vendor"))
        {
            toolCalled = "get_suppliers";
            var suppliers = await _erpTools.GetSuppliersAsync();
            dataResult = suppliers;
            var sb = new StringBuilder();
            sb.AppendLine($"### 🤝 Active Verified Suppliers");
            sb.AppendLine();
            sb.AppendLine($"Found **{suppliers.Count} registered supplier(s)** ready for procurement:");
            sb.AppendLine();
            sb.AppendLine("| Supplier Code | Company Name | Email Contact | Phone | Hub Location |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
            foreach (var s in suppliers)
            {
                sb.AppendLine($"| `{s.SupplierCode}` | **{s.Name}** | {s.Email} | {s.Phone} | {s.Address} |");
            }
            explanation = sb.ToString().Trim();
        }
        else if (prompt.Contains("purchase order") || prompt.Contains("po"))
        {
            toolCalled = "get_purchase_orders";
            var pos = await _erpTools.GetPurchaseOrdersAsync();
            dataResult = pos;
            var sb = new StringBuilder();
            sb.AppendLine($"### 📋 Procurement Purchase Orders");
            sb.AppendLine();
            sb.AppendLine($"Retrieved **{pos.Count} purchase order(s)** from system:");
            sb.AppendLine();
            sb.AppendLine("| PO Number | Status | Total Amount | Order Date |");
            sb.AppendLine("| :--- | :---: | :---: | :--- |");
            foreach (var po in pos)
            {
                sb.AppendLine($"| `{po.PONumber}` | {po.Status} | **${po.TotalAmount:F2}** | {po.OrderDate:yyyy-MM-dd} |");
            }
            explanation = sb.ToString().Trim();
        }
        else
        {
            toolCalled = "get_all_products_and_suppliers";
            var products = await _erpTools.GetAllProductsAsync();
            var suppliers = await _erpTools.GetSuppliersAsync();
            dataResult = new { products, suppliers };
            explanation = $"### 📊 ERP System Summary\n\n- **Total Products in Catalog:** **{products.Count}**\n- **Active Suppliers:** **{suppliers.Count}**\n\nAll microservices are operational.";
        }

        return (explanation, dataResult, toolCalled);
    }
}

public class ChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public Guid? ConversationGuid { get; set; }
}
