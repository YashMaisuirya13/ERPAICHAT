using System.ComponentModel.DataAnnotations;

namespace InventoryService.Models;

public class StockTransaction
{
    [Key]
    public int Id { get; set; }

    public Guid TransactionGuid { get; set; } = Guid.NewGuid();

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int InventoryItemId { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;

    public string TransactionType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public string? ReferenceNumber { get; set; }
}
