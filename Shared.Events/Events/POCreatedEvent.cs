using Shared.Events.Models;

namespace Shared.Events.Events;

public class POCreatedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid PurchaseOrderGuid { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid SupplierGuid { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public List<POItemDto> Items { get; set; } = new();
}
