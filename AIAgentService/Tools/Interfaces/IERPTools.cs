using AIAgentService.Tools.DTOs;

namespace AIAgentService.Tools.Interfaces;

public interface IERPTools
{
    // Inventory Tools
    Task<List<InventoryItemDto>> GetLowStockProductsAsync();
    Task<List<ProductItemDto>> GetAllProductsAsync();
    Task<ProductItemDto?> CreateProductAsync(string sku, string name, decimal unitPrice, int minimumStock);
    Task<List<InventoryItemDto>> GetProductInventoryAsync(int productId);
    Task<List<StockTransactionDto>> GetStockTransactionsAsync();

    // Supplier Tools
    Task<List<SupplierItemDto>> GetSuppliersAsync();
    Task<SupplierItemDto?> GetSupplierByGuidAsync(Guid supplierGuid);

    // Procurement Action Tools + Human Approval
    Task<PurchaseOrderResultDto?> CreateDraftPurchaseOrderAsync(Guid supplierGuid, List<PurchaseOrderItemResultDto> items);
    Task<PurchaseOrderResultDto?> SubmitPurchaseOrderForApprovalAsync(Guid purchaseOrderGuid);
    Task<PurchaseOrderResultDto?> ApprovePurchaseOrderAsync(Guid purchaseOrderGuid);
    Task<PurchaseOrderResultDto?> RejectPurchaseOrderAsync(Guid purchaseOrderGuid, string reason);
    Task<List<PurchaseOrderResultDto>> GetPurchaseOrdersAsync();
    Task<PurchaseOrderResultDto?> GetPurchaseOrderByGuidAsync(Guid purchaseOrderGuid);
}
