using System.Net.Http.Json;
using AIAgentService.Tools.DTOs;
using AIAgentService.Tools.Interfaces;

namespace AIAgentService.Tools;

public class ERPTools : IERPTools
{
    private readonly HttpClient _inventoryClient;
    private readonly HttpClient _supplierClient;
    private readonly HttpClient _procurementClient;
    private readonly ILogger<ERPTools> _logger;

    public ERPTools(
        IHttpClientFactory httpClientFactory,
        ILogger<ERPTools> logger)
    {
        _inventoryClient = httpClientFactory.CreateClient("InventoryService");
        _supplierClient = httpClientFactory.CreateClient("SupplierService");
        _procurementClient = httpClientFactory.CreateClient("ProcurementService");
        _logger = logger;
    }

    public async Task<List<InventoryItemDto>> GetLowStockProductsAsync()
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching low stock products from InventoryService...");
            var result = await _inventoryClient.GetFromJsonAsync<List<InventoryItemDto>>("/api/inventory/low-stock");
            return result ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch low stock products.");
            return new List<InventoryItemDto>();
        }
    }

    public async Task<List<ProductItemDto>> GetAllProductsAsync()
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching all products from InventoryService...");
            var result = await _inventoryClient.GetFromJsonAsync<List<ProductItemDto>>("/api/products");
            return result ?? new List<ProductItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch products.");
            return new List<ProductItemDto>();
        }
    }

    public async Task<List<InventoryItemDto>> GetProductInventoryAsync(int productId)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching inventory for product {ProductId}...", productId);
            var result = await _inventoryClient.GetFromJsonAsync<List<InventoryItemDto>>($"/api/products/{productId}/inventory");
            return result ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch product inventory.");
            return new List<InventoryItemDto>();
        }
    }

    public async Task<List<SupplierItemDto>> GetSuppliersAsync()
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching active suppliers from SupplierService...");
            var result = await _supplierClient.GetFromJsonAsync<List<SupplierItemDto>>("/api/suppliers");
            return result?.Where(s => s.IsActive).ToList() ?? new List<SupplierItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch suppliers.");
            return new List<SupplierItemDto>();
        }
    }

    public async Task<SupplierItemDto?> GetSupplierByGuidAsync(Guid supplierGuid)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching supplier {SupplierGuid}...", supplierGuid);
            return await _supplierClient.GetFromJsonAsync<SupplierItemDto>($"/api/suppliers/{supplierGuid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch supplier details.");
            return null;
        }
    }

    public async Task<PurchaseOrderResultDto?> CreateDraftPurchaseOrderAsync(Guid supplierGuid, List<PurchaseOrderItemResultDto> items)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Creating draft Purchase Order for Supplier {SupplierGuid} with {Count} items...", supplierGuid, items.Count);
            var payload = new
            {
                supplierId = 1,
                supplierGuid,
                items = items.Select(i => new
                {
                    productGuid = i.ProductGuid,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice
                })
            };

            var response = await _procurementClient.PostAsJsonAsync("/api/purchaseorders", payload);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Create PO failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PurchaseOrderResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create draft purchase order.");
            return null;
        }
    }

    public async Task<PurchaseOrderResultDto?> SubmitPurchaseOrderForApprovalAsync(Guid purchaseOrderGuid)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Submitting Purchase Order {POGuid} for approval...", purchaseOrderGuid);
            var response = await _procurementClient.PostAsync($"/api/purchaseorders/{purchaseOrderGuid}/submit", null);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PurchaseOrderResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit purchase order for approval.");
            return null;
        }
    }

    public async Task<PurchaseOrderResultDto?> ApprovePurchaseOrderAsync(Guid purchaseOrderGuid)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Approving Purchase Order {POGuid}...", purchaseOrderGuid);
            var response = await _procurementClient.PostAsync($"/api/purchaseorders/{purchaseOrderGuid}/approve", null);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PurchaseOrderResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve purchase order.");
            return null;
        }
    }

    public async Task<PurchaseOrderResultDto?> RejectPurchaseOrderAsync(Guid purchaseOrderGuid, string reason)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Rejecting Purchase Order {POGuid}...", purchaseOrderGuid);
            var response = await _procurementClient.PostAsJsonAsync($"/api/purchaseorders/{purchaseOrderGuid}/reject", new { reason });
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PurchaseOrderResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject purchase order.");
            return null;
        }
    }

    public async Task<List<PurchaseOrderResultDto>> GetPurchaseOrdersAsync()
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching purchase orders from ProcurementService...");
            var result = await _procurementClient.GetFromJsonAsync<List<PurchaseOrderResultDto>>("/api/purchaseorders");
            return result ?? new List<PurchaseOrderResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch purchase orders.");
            return new List<PurchaseOrderResultDto>();
        }
    }

    public async Task<PurchaseOrderResultDto?> GetPurchaseOrderByGuidAsync(Guid purchaseOrderGuid)
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching purchase order {POGuid} from ProcurementService...", purchaseOrderGuid);
            return await _procurementClient.GetFromJsonAsync<PurchaseOrderResultDto>($"/api/purchaseorders/{purchaseOrderGuid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch purchase order details.");
            return null;
        }
    }

    public async Task<List<StockTransactionDto>> GetStockTransactionsAsync()
    {
        try
        {
            _logger.LogInformation("[Tool Call] Fetching stock transactions from InventoryService...");
            var result = await _inventoryClient.GetFromJsonAsync<List<StockTransactionDto>>("/api/inventory/transactions");
            return result ?? new List<StockTransactionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch stock transactions.");
            return new List<StockTransactionDto>();
        }
    }
}
