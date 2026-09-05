using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs.Inventory;

public class StockOutRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string WarehouseCode { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }
}
