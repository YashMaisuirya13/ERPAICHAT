namespace ProcurementService.Clients.DTOs;

public class ProductDto
{
    public Guid ProductGuid { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
