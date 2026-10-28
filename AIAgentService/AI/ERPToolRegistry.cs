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
                    description = "Get active verified suppliers and contact info. Pass optional supplierName to search.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            supplierName = new
                            {
                                type = "string",
                                description = "Optional supplier name or code keyword (e.g. Lenovo, Dell, TechSupply). Omit for all."
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
                    name = "get_purchase_order_details",
                    description = "Get comprehensive details and line items for a specific purchase order by PO Number or ID.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            poNumber = new
                            {
                                type = "string",
                                description = "The purchase order code (e.g. PO-1001, PO-1002) or purchase order GUID."
                            }
                        },
                        required = new[] { "poNumber" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_stock_transactions",
                    description = "Get recent warehouse stock movements (Stock-In and Stock-Out audit logs) with timestamps, quantities, and reference notes.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productName = new
                            {
                                type = "string",
                                description = "Optional product name or SKU keyword filter (e.g. laptop, thinkpad, monitor). Omit for all."
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
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "approve_purchase_order",
                    description = "Approve a pending purchase order by PO Number (e.g. PO-2026-0005) or GUID.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            poNumber = new { type = "string", description = "The purchase order code (e.g. PO-2026-0005) or purchase order GUID to approve." }
                        },
                        required = new[] { "poNumber" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "create_product",
                    description = "Create a new product catalog item in the inventory system with SKU, name, unit price, and minimum safety stock threshold.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "Full descriptive name of the product (e.g. 'Ergonomic Mechanical Keyboard')." },
                            sku = new { type = "string", description = "Optional SKU code. If omitted, a clean SKU will be generated automatically based on the product name." },
                            unitPrice = new { type = "number", description = "Unit selling/cost price of the product (e.g. 89.99)." },
                            minimumStock = new { type = "integer", description = "Minimum safety stock threshold for low-stock reorder warnings (default is 10)." }
                        },
                        required = new[] { "name", "unitPrice" }
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
                string? filterName = null;
                if (root.HasValue && root.Value.TryGetProperty("supplierName", out var sNameProp))
                {
                    filterName = sNameProp.GetString()?.ToLowerInvariant();
                }

                var suppliers = await _erpTools.GetSuppliersAsync();
                var filtered = string.IsNullOrWhiteSpace(filterName)
                    ? suppliers
                    : suppliers.Where(s => s.Name.ToLower().Contains(filterName) || s.SupplierCode.ToLower().Contains(filterName)).ToList();

                var result = filtered.Select(s => new
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

            case "get_purchase_order_details":
            {
                string? targetPoNumber = null;
                if (root.HasValue && root.Value.TryGetProperty("poNumber", out var poProp))
                {
                    targetPoNumber = poProp.GetString()?.Trim();
                }

                if (string.IsNullOrWhiteSpace(targetPoNumber))
                {
                    var err = new { error = "Purchase Order number or ID is required." };
                    return (JsonSerializer.Serialize(err, s_jsonOptions), err);
                }

                var pos = await _erpTools.GetPurchaseOrdersAsync();
                var targetPo = pos.FirstOrDefault(p =>
                    p.PONumber.Equals(targetPoNumber, StringComparison.OrdinalIgnoreCase) ||
                    p.PurchaseOrderGuid.ToString().Equals(targetPoNumber, StringComparison.OrdinalIgnoreCase) ||
                    p.PONumber.ToLower().Contains(targetPoNumber.ToLower()));

                if (targetPo == null)
                {
                    var err = new { error = $"Purchase Order '{targetPoNumber}' was not found in the system." };
                    return (JsonSerializer.Serialize(err, s_jsonOptions), err);
                }

                // Fetch detailed PO with line items
                var detailedPo = await _erpTools.GetPurchaseOrderByGuidAsync(targetPo.PurchaseOrderGuid) ?? targetPo;

                // Resolve supplier name and product names for line items
                var suppliers = await _erpTools.GetSuppliersAsync();
                var supplier = suppliers.FirstOrDefault(s => s.SupplierGuid == detailedPo.SupplierGuid);
                var products = await _erpTools.GetAllProductsAsync();

                var lineItems = detailedPo.Items.Select(item =>
                {
                    var product = products.FirstOrDefault(p => p.ProductGuid == item.ProductGuid);
                    return new
                    {
                        productName = product?.Name ?? "Product Item",
                        sku = product?.SKU ?? "-",
                        quantity = item.Quantity,
                        unitPrice = item.UnitPrice,
                        totalAmount = item.TotalAmount
                    };
                }).ToList();

                var detailedResult = new
                {
                    poNumber = detailedPo.PONumber,
                    status = detailedPo.Status,
                    supplierName = supplier?.Name ?? "Verified Supplier",
                    supplierEmail = supplier?.Email ?? "-",
                    orderDate = detailedPo.OrderDate.ToString("yyyy-MM-dd"),
                    approvedAt = detailedPo.ApprovedAt?.ToString("yyyy-MM-dd HH:mm"),
                    rejectionReason = detailedPo.RejectionReason,
                    totalAmount = detailedPo.TotalAmount,
                    itemsCount = lineItems.Count,
                    items = lineItems
                };

                return (JsonSerializer.Serialize(detailedResult, s_jsonOptions), detailedResult);
            }

            case "get_stock_transactions":
            {
                string? query = null;
                if (root.HasValue && root.Value.TryGetProperty("productName", out var pProp))
                {
                    query = pProp.GetString()?.ToLowerInvariant();
                }

                var transactions = await _erpTools.GetStockTransactionsAsync();
                var filtered = string.IsNullOrWhiteSpace(query)
                    ? transactions
                    : transactions.Where(t => t.ProductName.ToLower().Contains(query) || t.SKU.ToLower().Contains(query)).ToList();

                var result = filtered
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(25)
                    .Select(t => new
                    {
                        productName = t.ProductName,
                        sku = t.SKU,
                        type = t.TransactionType,
                        quantity = t.Quantity,
                        warehouse = t.WarehouseCode,
                        date = t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                        referenceNumber = t.ReferenceNumber ?? "-"
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

            case "approve_purchase_order":
            {
                string? poIdentifier = null;
                if (root.HasValue && root.Value.TryGetProperty("poNumber", out var poProp))
                {
                    poIdentifier = poProp.GetString()?.Trim();
                }

                if (string.IsNullOrWhiteSpace(poIdentifier))
                {
                    return (JsonSerializer.Serialize(new { error = "Purchase order number or GUID is required." }, s_jsonOptions), new { error = "PO identifier required" });
                }

                Guid poGuid = Guid.Empty;
                if (!Guid.TryParse(poIdentifier, out poGuid))
                {
                    var allPos = await _erpTools.GetPurchaseOrdersAsync();
                    var match = allPos.FirstOrDefault(p => string.Equals(p.PONumber, poIdentifier, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        poGuid = match.PurchaseOrderGuid;
                    }
                }

                if (poGuid == Guid.Empty)
                {
                    return (JsonSerializer.Serialize(new { error = $"Could not find purchase order '{poIdentifier}'." }, s_jsonOptions), new { error = $"PO '{poIdentifier}' not found" });
                }

                var approved = await _erpTools.ApprovePurchaseOrderAsync(poGuid);
                if (approved == null)
                {
                    return (JsonSerializer.Serialize(new { error = $"Failed to approve purchase order '{poIdentifier}'. Ensure order is in PendingApproval status and caller has Manager/Admin permissions." }, s_jsonOptions), new { error = "Approval failed" });
                }

                var approveResult = new
                {
                    poNumber = approved.PONumber,
                    status = "Approved",
                    totalAmount = approved.TotalAmount,
                    approvedAt = approved.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                    message = $"Purchase order {approved.PONumber} has been successfully approved!"
                };

                return (JsonSerializer.Serialize(approveResult, s_jsonOptions), approveResult);
            }

            case "create_product":
            {
                string name = "";
                string sku = "";
                decimal unitPrice = 0;
                int minimumStock = 10;

                if (root.HasValue)
                {
                    if (root.Value.TryGetProperty("name", out var nProp))
                        name = nProp.GetString()?.Trim() ?? "";

                    if (root.Value.TryGetProperty("sku", out var sProp))
                        sku = sProp.GetString()?.Trim() ?? "";

                    if (root.Value.TryGetProperty("unitPrice", out var pProp))
                        pProp.TryGetDecimal(out unitPrice);

                    if (root.Value.TryGetProperty("minimumStock", out var mProp))
                        mProp.TryGetInt32(out minimumStock);
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return (JsonSerializer.Serialize(new { error = "Product name is required." }, s_jsonOptions), new { error = "Product name required" });
                }

                if (unitPrice <= 0)
                {
                    return (JsonSerializer.Serialize(new { error = "Valid unit price greater than 0 is required." }, s_jsonOptions), new { error = "Invalid unit price" });
                }

                if (string.IsNullOrWhiteSpace(sku))
                {
                    var prefix = new string(name.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(prefix)) prefix = "PROD";
                    sku = $"{prefix}-{Random.Shared.Next(100, 999)}";
                }

                if (minimumStock <= 0)
                {
                    minimumStock = 5;
                }

                var createdProduct = await _erpTools.CreateProductAsync(sku, name, unitPrice, minimumStock);
                if (createdProduct == null)
                {
                    return (JsonSerializer.Serialize(new { error = $"Failed to create product '{name}'. A product with SKU '{sku}' might already exist." }, s_jsonOptions), new { error = "Product creation failed" });
                }

                var productResult = new
                {
                    productId = createdProduct.Id,
                    productGuid = createdProduct.ProductGuid,
                    name = createdProduct.Name,
                    sku = createdProduct.SKU,
                    unitPrice = createdProduct.UnitPrice,
                    minimumStock = createdProduct.MinimumStock,
                    message = $"Product '{createdProduct.Name}' (SKU: {createdProduct.SKU}) was created successfully!"
                };

                return (JsonSerializer.Serialize(productResult, s_jsonOptions), productResult);
            }

            default:
                return (JsonSerializer.Serialize(new { message = $"Tool '{toolName}' is not recognized." }), new { message = $"Unrecognized tool: {toolName}" });
        }
    }
}
