using System.ComponentModel.DataAnnotations;

namespace InventoryService.Models;

public class InventoryItem
{
    [Key]
    public int Id { get; set; }

    public Guid InventoryGuid { get; set; } = Guid.NewGuid();
    public Product Product { get; set; } = null!;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;
}
