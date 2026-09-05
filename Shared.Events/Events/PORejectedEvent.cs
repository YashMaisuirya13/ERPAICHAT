namespace Shared.Events.Events;

public class PORejectedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid PurchaseOrderGuid { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
}
