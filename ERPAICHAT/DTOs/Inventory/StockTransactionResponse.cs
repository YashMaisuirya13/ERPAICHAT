namespace InventoryService.DTOs.Inventory;

public class StockTransactionResponse
{
    public Guid TransactionGuid { get; set; }
    public int ProductId { get; set; }
    public Guid ProductGuid { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int InventoryItemId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
}
