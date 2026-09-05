namespace Shared.Events.Events;

public class StockLowEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int ProductId { get; set; }
    public Guid ProductGuid { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int MinimumStock { get; set; }
    public int ShortageUnits => Math.Max(0, MinimumStock - CurrentQuantity);
}
