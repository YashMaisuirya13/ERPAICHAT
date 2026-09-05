namespace InventoryService.DTOs.Products;

public class ProductResponse
{
    public int Id { get; set; }
    public Guid ProductGuid { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int MinimumStock { get; set; }
}