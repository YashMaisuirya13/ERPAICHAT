using System.Text.Json;
using AIAgentService.Tools.DTOs;
using AIAgentService.Tools.Interfaces;

namespace AIAgentService.AI;

public interface IERPToolRegistry
{
    object[] GetToolDefinitions();
    Task<(string ResultJson, object DataResult)> ExecuteToolAsync(string toolName, string argumentsJson);
}

public class ERPToolRegistry : IERPToolRegistry
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IERPTools _erpTools;
    private readonly ILogger<ERPToolRegistry> _logger;

    public ERPToolRegistry(IERPTools erpTools, ILogger<ERPToolRegistry> logger)
    {
        _erpTools = erpTools;
        _logger = logger;
    }

    public object[] GetToolDefinitions()
    {
        return new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "get_inventory",
                    description = "Get warehouse inventory stock levels, prices, and locations. Pass productName to filter.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productName = new
                            {
                                type = "string",
                                description = "Product keyword (e.g. laptop, thinkpad, monitor, mouse, switch). Omit for all."
                            }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_low_stock_alerts",
                    description = "Get products currently at or below safety reorder threshold.",
                    parameters = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_suppliers",
                    description = "Get active verified suppliers and contact info.",
                    parameters = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_purchase_orders",
                    description = "Get purchase orders. Optional status filter: Draft, Submitted, Approved, Received.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                description = "Filter by status: Draft, Submitted, Approved, or Received."
                            }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "create_purchase_order",
                    description = "Create and submit a purchase order for approval.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productName = new { type = "string", description = "Product name (e.g. ThinkPad, Dell Laptop)." },
                            quantity = new { type = "integer", description = "Quantity to order." },
                            supplierName = new { type = "string", description = "Optional preferred supplier name." }
                        },
                        required = new[] { "productName", "quantity" }
                    }
                }
            }
        };
    }

    public async Task<(string ResultJson, object DataResult)> ExecuteToolAsync(string toolName, string argumentsJson)
    {
        _logger.LogInformation("[ERPToolRegistry] Executing Tool: {ToolName} with Args: {Args}", toolName, argumentsJson);

        using var argsDoc = string.IsNullOrWhiteSpace(argumentsJson) ? null : JsonDocument.Parse(argumentsJson);
        var root = argsDoc?.RootElement;

        switch (toolName.ToLowerInvariant())
        {
            case "get_low_stock_alerts":
            {
                var lowStock = await _erpTools.GetLowStockProductsAsync();
                var result = lowStock.Select(i => new
                {
                    productName = i.ProductName,
                    sku = i.SKU,
                    availableQuantity = i.Quantity,
                    warehouse = i.WarehouseCode,
                    reorderThreshold = i.MinimumStock,
                    status = "Low Stock Alert"
                }).ToList();
                return (JsonSerializer.Serialize(result, s_jsonOptions), result);
            }

            case "get_inventory":
            {
                string? query = null;
                if (root.HasValue && root.Value.TryGetProperty("productName", out var pProp))
                {
                    query = pProp.GetString()?.ToLowerInvariant();
                }

                var products = await _erpTools.GetAllProductsAsync();
                var targetProducts = (string.IsNullOrWhiteSpace(query)
                    ? products
                    : products.Where(p => p.Name.ToLower().Contains(query) || p.SKU.ToLower().Contains(query)))
                    .Take(25)
                    .ToList();

                var inventoryList = new List<object>();
                foreach (var p in targetProducts)
                {
                    var inv = await _erpTools.GetProductInventoryAsync(p.Id);
                    var totalQty = inv.Sum(i => i.Quantity);
                    var whList = inv.Select(i => new { warehouse = i.WarehouseCode, quantity = i.Quantity }).ToList();
                    var isLowStock = totalQty <= p.MinimumStock;

                    inventoryList.Add(new
                    {
                        id = p.Id,
                        productName = p.Name,
                        sku = p.SKU,
                        unitPrice = p.UnitPrice,
                        totalAvailable = totalQty,
                        reorderThreshold = p.MinimumStock,
                        status = isLowStock ? "Low Stock" : "Healthy",
                        warehouses = whList
                    });
                }

                return (JsonSerializer.Serialize(inventoryList, s_jsonOptions), inventoryList);
            }

            case "get_suppliers":
            {
                var suppliers = await _erpTools.GetSuppliersAsync();
                var result = suppliers.Select(s => new
                {
                    supplierCode = s.SupplierCode,
                    name = s.Name,
                    email = s.Email,
                    phone = s.Phone,
                    address = s.Address,
                    supplierGuid = s.SupplierGuid
                }).ToList();
                return (JsonSerializer.Serialize(result, s_jsonOptions), result);
            }

            case "get_purchase_orders":
            {
                string? statusFilter = null;
                if (root.HasValue && root.Value.TryGetProperty("status", out var sProp))
                {
                    statusFilter = sProp.GetString()?.ToLowerInvariant();
                }

                var pos = await _erpTools.GetPurchaseOrdersAsync();
                var filtered = string.IsNullOrWhiteSpace(statusFilter)
                    ? pos
                    : pos.Where(po => po.Status.ToLower() == statusFilter).ToList();

                var result = filtered.Select(po => new
                {
                    poNumber = po.PONumber,
                    status = po.Status,
                    totalAmount = po.TotalAmount,
                    orderDate = po.OrderDate.ToString("yyyy-MM-dd"),
                    guid = po.PurchaseOrderGuid
                }).ToList();

                return (JsonSerializer.Serialize(result, s_jsonOptions), result);
            }

            case "create_purchase_order":
            {
                string productName = "ThinkPad";
                int quantity = 20;
                string? preferredSupplier = null;

                if (root.HasValue)
                {
                    if (root.Value.TryGetProperty("productName", out var pn)) productName = pn.GetString() ?? productName;
                    if (root.Value.TryGetProperty("quantity", out var q)) quantity = q.GetInt32();
                    if (root.Value.TryGetProperty("supplierName", out var sn)) preferredSupplier = sn.GetString();
                }

                var products = await _erpTools.GetAllProductsAsync();
                var targetProduct = products.FirstOrDefault(p => p.Name.ToLower().Contains(productName.ToLower()) || p.SKU.ToLower().Contains(productName.ToLower()))
                    ?? products.FirstOrDefault();

                var suppliers = await _erpTools.GetSuppliersAsync();
                var targetSupplier = !string.IsNullOrWhiteSpace(preferredSupplier)
                    ? suppliers.FirstOrDefault(s => s.Name.ToLower().Contains(preferredSupplier.ToLower()))
                    : suppliers.FirstOrDefault();

                if (targetSupplier == null)
                {
                    return (JsonSerializer.Serialize(new { error = "No active supplier found in registry to place order." }, s_jsonOptions), new { error = "No active supplier found" });
                }

                Guid productGuid = targetProduct?.ProductGuid ?? Guid.NewGuid();
                decimal unitPrice = targetProduct?.UnitPrice ?? 1499.00m;

                var items = new List<PurchaseOrderItemResultDto>
                {
                    new PurchaseOrderItemResultDto
                    {
                        ProductGuid = productGuid,
                        Quantity = quantity,
                        UnitPrice = unitPrice
                    }
                };

                var draftPo = await _erpTools.CreateDraftPurchaseOrderAsync(targetSupplier.SupplierGuid, items);
                if (draftPo == null)
                {
                    return (JsonSerializer.Serialize(new { error = "Failed to create draft purchase order in database." }, s_jsonOptions), new { error = "Failed to create draft purchase order" });
                }

                var submittedPo = await _erpTools.SubmitPurchaseOrderForApprovalAsync(draftPo.PurchaseOrderGuid);
                string poCode = !string.IsNullOrWhiteSpace(submittedPo?.PONumber) ? submittedPo.PONumber : $"PO-{draftPo.PurchaseOrderGuid.ToString().Substring(0, 8).ToUpper()}";

                var creationResult = new
                {
                    poNumber = poCode,
                    status = "Submitted",
                    supplier = targetSupplier.Name,
                    product = targetProduct?.Name ?? productName,
                    quantity,
                    unitPrice,
                    totalAmount = unitPrice * quantity,
                    message = $"Purchase order {poCode} registered and submitted for manager approval."
                };

                return (JsonSerializer.Serialize(creationResult, s_jsonOptions), creationResult);
            }

            default:
                return (JsonSerializer.Serialize(new { message = $"Tool '{toolName}' is not recognized." }), new { message = $"Unrecognized tool: {toolName}" });
        }
    }
}
