using AIAgentService.Tools.DTOs;

namespace AIAgentService.Tools.Interfaces;

public interface IERPTools
{
    // Inventory Tools
    Task<List<InventoryItemDto>> GetLowStockProductsAsync();
    Task<List<ProductItemDto>> GetAllProductsAsync();
    Task<ProductItemDto?> CreateProductAsync(string sku, string name, decimal unitPrice, int minimumStock);
    Task<List<InventoryItemDto>> GetProductInventoryAsync(int productId);
    Task<InventoryItemDto?> StockInAsync(int productId, int quantity, string warehouseCode, string? referenceNumber = null);
    Task<InventoryItemDto?> StockOutAsync(int productId, int quantity, string warehouseCode, string? referenceNumber = null);
    Task<List<StockTransactionDto>> GetStockTransactionsAsync();

    // Supplier Tools
    Task<List<SupplierItemDto>> GetSuppliersAsync();
    Task<SupplierItemDto?> GetSupplierByGuidAsync(Guid supplierGuid);
    Task<SupplierItemDto?> CreateSupplierAsync(string supplierCode, string name, string email, string phone, string address);

    // Procurement Action Tools + Human Approval
    Task<PurchaseOrderResultDto?> CreateDraftPurchaseOrderAsync(Guid supplierGuid, List<PurchaseOrderItemResultDto> items);
    Task<PurchaseOrderResultDto?> SubmitPurchaseOrderForApprovalAsync(Guid purchaseOrderGuid);
    Task<PurchaseOrderResultDto?> ApprovePurchaseOrderAsync(Guid purchaseOrderGuid);
    Task<PurchaseOrderResultDto?> RejectPurchaseOrderAsync(Guid purchaseOrderGuid, string reason);
    Task<List<PurchaseOrderResultDto>> GetPurchaseOrdersAsync();
    Task<PurchaseOrderResultDto?> GetPurchaseOrderByGuidAsync(Guid purchaseOrderGuid);
}
