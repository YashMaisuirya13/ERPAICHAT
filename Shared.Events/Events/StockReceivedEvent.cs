namespace Shared.Events.Events;

public class StockReceivedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int ProductId { get; set; }
    public Guid ProductGuid { get; set; }
    public int Quantity { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public Guid? PurchaseOrderGuid { get; set; }
}
