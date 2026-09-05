using AIAgentService.Tools.DTOs;
using AIAgentService.Tools.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IERPTools _erpTools;
    private readonly ILogger<AIController> _logger;

    public AIController(IERPTools erpTools, ILogger<AIController> logger)
    {
        _erpTools = erpTools;
        _logger = logger;
    }

    [HttpPost("chat")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            return BadRequest(new { message = "Prompt is required." });
        }

        var prompt = request.Prompt.ToLowerInvariant();
        _logger.LogInformation("AI Agent received prompt: {Prompt}", request.Prompt);

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
                explanation = $"Laptop stock is {available}, while minimum stock is {laptop.MinimumStock}.";
                dataResult = new { laptop, available, inventory = inv };
            }
            else
            {
                var lowStock = await _erpTools.GetLowStockProductsAsync();
                explanation = $"Checked stock levels via `get_low_stock_products()`. Found {lowStock.Count} low stock item(s).";
                dataResult = lowStock;
            }
        }
        else if (prompt.Contains("prepare a po") || prompt.Contains("find suppliers and prepare") || prompt.Contains("create po") || prompt.Contains("po for 20"))
        {
            toolCalled = "get_suppliers() -> create_purchase_order() -> submit_purchase_order_for_approval()";
            var suppliers = await _erpTools.GetSuppliersAsync();
            if (suppliers.Count == 0)
            {
                explanation = "I searched for suppliers using `get_suppliers()`, but no active suppliers were found in the registry.";
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
                    explanation = $"I found {suppliers.Count} suppliers. I've prepared PO-{poCode} for 20 laptops with {supplier.Name} and submitted it for approval.";
                    dataResult = submittedPo ?? draftPo;
                }
                else
                {
                    explanation = $"I found {suppliers.Count} suppliers, but failed to create the draft purchase order.";
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
                explanation = "Checked inventory via `get_low_stock_products()`. Currently, all product stock levels are healthy with zero items below reorder thresholds.";
            }
            else
            {
                var itemsList = string.Join(", ", lowStock.Select(i => $"Product SKU {i.SKU} (Available: {i.Quantity}, Reorder Level: {i.MinimumStock})"));
                explanation = $"Executed `get_low_stock_products()`. Found {lowStock.Count} low stock product(s): {itemsList}.";
            }
        }
        else if (prompt.Contains("supplier") || prompt.Contains("vendor"))
        {
            toolCalled = "get_suppliers";
            var suppliers = await _erpTools.GetSuppliersAsync();
            dataResult = suppliers;
            var supList = string.Join(", ", suppliers.Select(s => $"{s.Name} ({s.SupplierCode})"));
            explanation = $"Executed `get_suppliers()`. Found {suppliers.Count} active verified supplier(s): {supList}.";
        }
        else if (prompt.Contains("purchase order") || prompt.Contains("po"))
        {
            toolCalled = "get_purchase_orders";
            var pos = await _erpTools.GetPurchaseOrdersAsync();
            dataResult = pos;
            var poSummary = string.Join(", ", pos.Select(po => $"PO #{po.PurchaseOrderGuid.ToString().Substring(0, 8)} (Status: {po.Status}, Total: ${po.TotalAmount})"));
            explanation = $"Executed `get_purchase_orders()`. Retrieved {pos.Count} purchase order(s): {poSummary}.";
        }
        else
        {
            toolCalled = "get_all_products_and_suppliers";
            var products = await _erpTools.GetAllProductsAsync();
            var suppliers = await _erpTools.GetSuppliersAsync();
            dataResult = new { products, suppliers };
            explanation = $"Processed prompt using ERP AI agent tools. Retrieved {products.Count} products and {suppliers.Count} suppliers from system services.";
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                response = explanation,
                toolCalled,
                result = dataResult
            }
        });
    }
}

public class ChatRequest
{
    public string Prompt { get; set; } = string.Empty;
}
