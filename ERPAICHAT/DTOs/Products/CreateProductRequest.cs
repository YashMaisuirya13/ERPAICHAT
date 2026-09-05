using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs.Products;

public class CreateProductRequest
{
    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumStock { get; set; }
}
