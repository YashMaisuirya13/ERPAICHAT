namespace InventoryService.DTOs.Inventory;

public class InventoryResponse
{
    public int Id { get; set; }

    public Guid InventoryGuid { get; set; }

    public int ProductId { get; set; }

    public Guid ProductGuid { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public int MinimumStock { get; set; }

    public bool IsLowStock { get; set; }
}
