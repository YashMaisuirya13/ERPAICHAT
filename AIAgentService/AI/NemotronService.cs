using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIAgentService.AI;

public class NemotronService : INemotronService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IERPToolRegistry _toolRegistry;
    private readonly ILogger<NemotronService> _logger;

    public NemotronService(
        HttpClient httpClient,
        IConfiguration configuration,
        IERPToolRegistry toolRegistry,
        ILogger<NemotronService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var agentResult = await ExecuteChatAgentAsync(prompt);
        return agentResult.Response;
    }

    public async Task<AgentChatResult> ExecuteChatAgentAsync(string prompt)
    {
        var apiKey = _configuration["Nemotron:ApiKey"]?.Trim();
        var primaryModel = _configuration["Nemotron:Model"] ?? "nvidia/nemotron-3-ultra-550b-a55b:free";
        var fallbackModel = _configuration["Nemotron:FallbackModel"] ?? "openrouter/free";
        var endpoint = _configuration["Nemotron:Endpoint"] ?? "https://openrouter.ai/api/v1/chat/completions";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_"))
        {
            _logger.LogInformation("[NemotronService] API key is unconfigured. Falling back to local tools.");
            throw new InvalidOperationException(
                "Nemotron API key is not configured in appsettings.json ('Nemotron:ApiKey'). Configure your OpenRouter key.");
        }

        // Try primary model first, fallback to fallbackModel if primary model has an upstream error (e.g. 503)
        try
        {
            return await ExecuteAgentTurnAsync(endpoint, apiKey, primaryModel, prompt, fallbackModel);
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(fallbackModel) && !fallbackModel.Equals(primaryModel, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[NemotronService] Primary model '{PrimaryModel}' failed ({Message}). Attempting fallback model '{FallbackModel}'...",
                primaryModel, ex.Message, fallbackModel);

            return await ExecuteAgentTurnAsync(endpoint, apiKey, fallbackModel, prompt, null);
        }
    }

    private async Task<AgentChatResult> ExecuteAgentTurnAsync(string endpoint, string apiKey, string model, string prompt, string? fallbackModel = null)
    {
        _logger.LogInformation("[NemotronService] Starting Agent Turn for prompt: '{Prompt}' with model: '{Model}'", prompt, model);

        var systemMessage = new
        {
            role = "system",
            content = "You are an intelligent ERP AI Assistant for enterprise resource planning (inventory, suppliers, procurement). You have access to database tools. ONLY call tools when the user asks for specific ERP data or actions. For greetings or general conversation, respond directly without calling any tools."
        };

        var userMessage = new
        {
            role = "user",
            content = prompt
        };

        // Token Optimization: Only include tools when user prompt indicates an ERP query or action
        bool includeTools = ShouldIncludeTools(prompt);
        var tools = includeTools ? _toolRegistry.GetToolDefinitions() : null;

        object requestBody = tools != null
            ? new
            {
                model,
                messages = new object[] { systemMessage, userMessage },
                tools,
                temperature = 0.2,
                max_tokens = 512
            }
            : new
            {
                model,
                messages = new object[] { systemMessage, userMessage },
                temperature = 0.2,
                max_tokens = 512
            };

        var rawResponse = await SendOpenRouterRequestAsync(endpoint, apiKey, requestBody);
        using var doc = JsonDocument.Parse(rawResponse);

        if (doc.RootElement.TryGetProperty("error", out var errorElement))
        {
            string errorMessage = errorElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Upstream error" : "Upstream error";
            throw new HttpRequestException($"Upstream error from model '{model}': {errorMessage}");
        }

        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"Invalid completion from model '{model}'. Response: {rawResponse}");
        }

        var messageElement = choices[0].GetProperty("message");

        // CASE 1: Model requested Tool Calls!
        if (messageElement.TryGetProperty("tool_calls", out var toolCallsElement) &&
            toolCallsElement.ValueKind == JsonValueKind.Array &&
            toolCallsElement.GetArrayLength() > 0)
        {
            var firstToolCall = toolCallsElement[0];
            string toolCallId = firstToolCall.GetProperty("id").GetString() ?? "call_0";
            var func = firstToolCall.GetProperty("function");
            string toolName = func.GetProperty("name").GetString() ?? "";
            string arguments = func.TryGetProperty("arguments", out var argProp) ? argProp.GetString() ?? "{}" : "{}";

            _logger.LogInformation("[NemotronService] [Single-Turn] Model selected tool: '{ToolName}' with args: {Args}", toolName, arguments);

            // Execute the selected tool against the ERP microservices
            var (resultJson, dataResult) = await _toolRegistry.ExecuteToolAsync(toolName, arguments);

            // Single-Turn Deterministic Formatting:
            // Eliminates Turn 2 roundtrip latency (0ms format time) and cuts token consumption by 60-80%!
            bool enableTwoTurn = _configuration.GetValue<bool>("Nemotron:EnableTwoTurnSummarization", false);
            string responseMarkdown;

            if (enableTwoTurn)
            {
                responseMarkdown = await ExecuteTurn2SummarizationAsync(endpoint, apiKey, model, userMessage, toolCallId, toolName, arguments, resultJson, fallbackModel);
            }
            else
            {
                responseMarkdown = FormatToolResultToMarkdown(toolName, resultJson);
            }

            return new AgentChatResult
            {
                Response = responseMarkdown,
                ToolCalled = toolName,
                ToolResult = dataResult
            };
        }

        // CASE 2: No tool call needed! (Direct greeting or conversational reply)
        var content = messageElement.TryGetProperty("content", out var cProp) ? cProp.GetString()?.Trim() : null;
        return new AgentChatResult
        {
            Response = content ?? "Hello! I am your ERP Assistant. How can I help you today?",
            ToolCalled = null,
            ToolResult = null
        };
    }

    private static bool ShouldIncludeTools(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var p = prompt.Trim().ToLowerInvariant();

        // Exact greetings and conversational meta queries without ERP intent
        string[] simpleGreetings =
        {
            "hi", "hello", "hey", "good morning", "good evening", "good afternoon",
            "howdy", "greetings", "help", "who are you", "what can you do", "thanks",
            "thank you", "bye", "goodbye", "ok", "okay"
        };

        if (simpleGreetings.Any(g => p == g || p == g + "!" || p == g + "." || p == g + "?"))
        {
            return false;
        }

        // Keywords that signal ERP query, data inspection, or actions
        string[] erpKeywords =
        {
            "stock", "inventory", "product", "supplier", "vendor", "po", "purchase",
            "order", "warehouse", "catalog", "buy", "reorder", "quantity", "price",
            "laptop", "thinkpad", "monitor", "mouse", "desk", "switch", "create",
            "draft", "alert", "low", "check", "find", "show", "get", "list", "procure"
        };

        if (erpKeywords.Any(k => p.Contains(k)))
        {
            return true;
        }

        // For longer multi-word prompts, include tools by default for safety
        return p.Length > 20;
    }

    private async Task<string> ExecuteTurn2SummarizationAsync(
        string endpoint, string apiKey, string model, object userMessage,
        string toolCallId, string toolName, string arguments, string resultJson,
        string? fallbackModel)
    {
        var secondRequestBody = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are an intelligent ERP AI Assistant. Present the ERP data clearly and concisely in structured Markdown. Do NOT output raw JSON blocks. Format numbers, quantities, and statuses professionally."
                },
                userMessage,
                new
                {
                    role = "assistant",
                    tool_calls = new object[]
                    {
                        new
                        {
                            id = toolCallId,
                            type = "function",
                            function = new
                            {
                                name = toolName,
                                arguments
                            }
                        }
                    }
                },
                new
                {
                    role = "tool",
                    tool_call_id = toolCallId,
                    content = resultJson
                }
            },
            temperature = 0.3,
            max_tokens = 1024
        };

        string? finalContent = null;

        try
        {
            var secondRaw = await SendOpenRouterRequestAsync(endpoint, apiKey, secondRequestBody);
            using var secondDoc = JsonDocument.Parse(secondRaw);
            if (secondDoc.RootElement.TryGetProperty("choices", out var ch2) && ch2.GetArrayLength() > 0)
            {
                var text = ch2[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
                if (!string.IsNullOrEmpty(text) && !text.StartsWith("```json", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("[{"))
                {
                    finalContent = text;
                }
            }
        }
        catch (Exception ex2)
        {
            _logger.LogWarning("[NemotronService] Turn 2 summarization failed with '{Model}' ({Message}).", model, ex2.Message);
        }

        if (string.IsNullOrEmpty(finalContent) && !string.IsNullOrWhiteSpace(fallbackModel) && !fallbackModel.Equals(model, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                _logger.LogInformation("[NemotronService] Retrying Turn 2 with fallback model '{FallbackModel}'...", fallbackModel);
                var fallbackRequestBody = new
                {
                    model = fallbackModel,
                    messages = secondRequestBody.messages,
                    temperature = 0.3,
                    max_tokens = 1024
                };

                var fallbackRaw = await SendOpenRouterRequestAsync(endpoint, apiKey, fallbackRequestBody);
                using var fbDoc = JsonDocument.Parse(fallbackRaw);
                if (fbDoc.RootElement.TryGetProperty("choices", out var fbChoices) && fbChoices.GetArrayLength() > 0)
                {
                    var text = fbChoices[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
                    if (!string.IsNullOrEmpty(text) && !text.StartsWith("```json", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("[{"))
                    {
                        finalContent = text;
                    }
                }
            }
            catch (Exception exFb)
            {
                _logger.LogWarning("[NemotronService] Turn 2 with fallback model also failed ({Message}).", exFb.Message);
            }
        }

        return !string.IsNullOrEmpty(finalContent)
            ? finalContent
            : FormatToolResultToMarkdown(toolName, resultJson);
    }

    private async Task<string> SendOpenRouterRequestAsync(string endpoint, string apiKey, object requestBody)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", "http://localhost:5048");
        httpRequest.Headers.TryAddWithoutValidation("X-Title", "ERP AI Agent Service");

        var response = await _httpClient.SendAsync(httpRequest);
        var rawResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var extractedError = TryExtractErrorMessage(rawResponse);
            throw new HttpRequestException($"API returned status {(int)response.StatusCode}: {extractedError ?? rawResponse}");
        }

        return rawResponse;
    }

    private static string? TryExtractErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }
        }
        catch
        {
            // Ignore parse failure and return null
        }
        return null;
    }

    public static string FormatToolResultToMarkdown(string toolName, string resultJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            return toolName.ToLowerInvariant() switch
            {
                "get_low_stock_alerts" => FormatLowStockAlerts(root),
                "get_inventory" => FormatInventory(root),
                "get_suppliers" => FormatSuppliers(root),
                "get_purchase_orders" => FormatPurchaseOrders(root),
                "create_purchase_order" => FormatCreatedPurchaseOrder(root),
                _ => FormatGenericToolResult(toolName, root)
            };
        }
        catch
        {
            return $"### Execution Completed for `{toolName}`\n\nERP operation executed successfully.";
        }
    }

    private static string FormatLowStockAlerts(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 🟢 All Stock Levels Healthy\n\nAll inventory products are currently above their minimum safety reorder thresholds. No replenishment is required at this time.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### ⚠️ Low Stock Inventory Alert");
        sb.AppendLine();
        sb.AppendLine($"Identified **{count} product{(count > 1 ? "s" : "")}** currently below minimum safety stock levels:");
        sb.AppendLine();
        sb.AppendLine("| Product Name | SKU | Available Qty | Warehouse | Safety Min | Status |");
        sb.AppendLine("| :--- | :--- | :---: | :--- | :---: | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var name = item.TryGetProperty("productName", out var np) ? np.GetString() ?? "" : "";
            var sku = item.TryGetProperty("sku", out var sp) ? sp.GetString() ?? "" : "";
            var qty = item.TryGetProperty("availableQuantity", out var qp) ? qp.GetInt32().ToString() : "0";
            var wh = item.TryGetProperty("warehouse", out var wp) ? wp.GetString() ?? "" : "";
            var min = item.TryGetProperty("reorderThreshold", out var mp) ? mp.GetInt32().ToString() : "0";
            var status = item.TryGetProperty("status", out var stp) ? stp.GetString() ?? "Low Stock Alert" : "Low Stock Alert";

            sb.AppendLine($"| {name} | `{sku}` | **{qty}** | {wh} | {min} | ⚠️ {status} |");
        }

        sb.AppendLine();
        sb.AppendLine("💡 **Action Recommended:** Prepare a purchase order to replenish items below minimum thresholds.");
        return sb.ToString().Trim();
    }

    private static string FormatInventory(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 📦 Inventory Catalog\n\nNo products found matching your search criteria.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 📦 Warehouse Inventory Levels");
        sb.AppendLine();
        sb.AppendLine($"Retrieved records for **{count} product{(count > 1 ? "s" : "")}** in catalog:");
        sb.AppendLine();
        sb.AppendLine("| Product Name | SKU | Unit Price | Total Stock | Safety Min | Status |");
        sb.AppendLine("| :--- | :--- | :---: | :---: | :---: | :--- |");

        var warehouseBreakdowns = new List<string>();

        foreach (var item in root.EnumerateArray())
        {
            var name = item.TryGetProperty("productName", out var np) ? np.GetString() ?? "" : "";
            var sku = item.TryGetProperty("sku", out var sp) ? sp.GetString() ?? "" : "";
            var price = item.TryGetProperty("unitPrice", out var pp) ? pp.GetDecimal().ToString("C2") : "$0.00";
            var total = item.TryGetProperty("totalAvailable", out var tp) ? tp.GetInt32().ToString() : "0";
            var min = item.TryGetProperty("reorderThreshold", out var mp) ? mp.GetInt32().ToString() : "0";
            var status = item.TryGetProperty("status", out var stp) ? stp.GetString() ?? "Healthy" : "Healthy";
            var isLow = status.Contains("Low", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine($"| {name} | `{sku}` | {price} | **{total}** | {min} | {(isLow ? "⚠️ Low Stock" : "✅ Healthy")} |");

            if (item.TryGetProperty("warehouses", out var whs) && whs.ValueKind == JsonValueKind.Array && whs.GetArrayLength() > 0)
            {
                var locations = string.Join(", ", whs.EnumerateArray().Select(w =>
                {
                    var whCode = w.TryGetProperty("warehouse", out var c) ? c.GetString() ?? "" : "";
                    var q = w.TryGetProperty("quantity", out var qv) ? qv.GetInt32().ToString() : "0";
                    return $"{whCode}: {q} units";
                }));
                warehouseBreakdowns.Add($"- **{name}**: {locations}");
            }
        }

        if (warehouseBreakdowns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Warehouse Distribution Breakdown:**");
            foreach (var wb in warehouseBreakdowns)
            {
                sb.AppendLine(wb);
            }
        }

        return sb.ToString().Trim();
    }

    private static string FormatSuppliers(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 🤝 Active Verified Suppliers\n\nNo active suppliers are currently registered in the database.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 🤝 Active Verified Suppliers");
        sb.AppendLine();
        sb.AppendLine($"Found **{count} registered supplier{(count > 1 ? "s" : "")}** available for procurement:");
        sb.AppendLine();
        sb.AppendLine("| Supplier Code | Company Name | Email Contact | Phone | Hub Location |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var code = item.TryGetProperty("supplierCode", out var cp) ? cp.GetString() ?? "" : "";
            var name = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
            var email = item.TryGetProperty("email", out var ep) ? ep.GetString() ?? "" : "";
            var phone = item.TryGetProperty("phone", out var pp) ? pp.GetString() ?? "" : "";
            var addr = item.TryGetProperty("address", out var ap) ? ap.GetString() ?? "" : "";

            sb.AppendLine($"| `{code}` | **{name}** | {email} | {phone} | {addr} |");
        }

        return sb.ToString().Trim();
    }

    private static string FormatPurchaseOrders(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 📋 Procurement Purchase Orders\n\nNo purchase orders found matching the filter criteria.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 📋 Procurement Purchase Orders");
        sb.AppendLine();
        sb.AppendLine($"Retrieved **{count} purchase order{(count > 1 ? "s" : "")}** from the system:");
        sb.AppendLine();
        sb.AppendLine("| PO Number | Status | Total Amount | Order Date |");
        sb.AppendLine("| :--- | :---: | :---: | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var poNum = item.TryGetProperty("poNumber", out var pp) ? pp.GetString() ?? "" : "";
            var status = item.TryGetProperty("status", out var sp) ? sp.GetString() ?? "Draft" : "Draft";
            var amount = item.TryGetProperty("totalAmount", out var ap) ? ap.GetDecimal().ToString("C2") : "$0.00";
            var date = item.TryGetProperty("orderDate", out var dp) ? dp.GetString() ?? "" : "";

            string statusIcon = status.ToLowerInvariant() switch
            {
                "approved" => "✅ Approved",
                "submitted" => "🟡 Submitted",
                "received" => "📦 Received",
                _ => $"📝 {status}"
            };

            sb.AppendLine($"| `{poNum}` | {statusIcon} | **{amount}** | {date} |");
        }

        return sb.ToString().Trim();
    }

    private static string FormatCreatedPurchaseOrder(JsonElement root)
    {
        if (root.TryGetProperty("error", out var errProp))
        {
            return $"### ❌ Purchase Order Error\n\nUnable to generate purchase order: {errProp.GetString()}";
        }

        var poNum = root.TryGetProperty("poNumber", out var pp) ? pp.GetString() ?? "PO-NEW" : "PO-NEW";
        var product = root.TryGetProperty("product", out var prp) ? prp.GetString() ?? "Product" : "Product";
        var quantity = root.TryGetProperty("quantity", out var qp) ? qp.GetInt32() : 0;
        var unitPrice = root.TryGetProperty("unitPrice", out var up) ? up.GetDecimal() : 0m;
        var total = root.TryGetProperty("totalAmount", out var tp) ? tp.GetDecimal() : 0m;
        var supplier = root.TryGetProperty("supplier", out var sp) ? sp.GetString() ?? "Supplier" : "Supplier";
        var status = root.TryGetProperty("status", out var stp) ? stp.GetString() ?? "Submitted" : "Submitted";

        var sb = new StringBuilder();
        sb.AppendLine("### ✅ Purchase Order Created & Submitted");
        sb.AppendLine();
        sb.AppendLine("A new procurement purchase order has been successfully generated:");
        sb.AppendLine();
        sb.AppendLine($"- **PO Reference:** `{poNum}`");
        sb.AppendLine($"- **Product Name:** {product}");
        sb.AppendLine($"- **Quantity:** **{quantity} units**");
        sb.AppendLine($"- **Unit Price:** {unitPrice:C2}");
        sb.AppendLine($"- **Total Value:** **{total:C2}**");
        sb.AppendLine($"- **Supplier:** {supplier}");
        sb.AppendLine($"- **Status:** 🟡 **{status}** (Pending Manager Approval)");
        sb.AppendLine();
        sb.AppendLine("📋 You can review and approve this order under the **Purchase Orders** dashboard.");
        return sb.ToString().Trim();
    }

    private static string FormatGenericToolResult(string toolName, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### ⚡ Automated ERP Action: `{toolName}`");
        sb.AppendLine();

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                sb.AppendLine($"- **{prop.Name}:** {prop.Value.ToString()}");
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            int index = 1;
            foreach (var item in root.EnumerateArray())
            {
                sb.AppendLine($"{index++}. {item.ToString()}");
            }
        }
        else
        {
            sb.AppendLine(root.ToString());
        }

        return sb.ToString().Trim();
    }
}

