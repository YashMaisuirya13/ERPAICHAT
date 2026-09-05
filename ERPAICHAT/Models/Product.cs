using System.ComponentModel.DataAnnotations;

namespace InventoryService.Models;

public class Product
{
    [Key]
    public int Id { get; set; }
    public Guid ProductGuid { get; set; } = Guid.NewGuid();

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int MinimumStock { get; set; }
    public ICollection<InventoryItem> InventoryItems { get; set; }
      = new List<InventoryItem>();
}
