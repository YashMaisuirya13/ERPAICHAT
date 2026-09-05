namespace ProcurementService.DTOs.PurchaseOrders;

public class PurchaseOrderResponse
{
    public int Id { get; set; }
    public Guid PurchaseOrderGuid { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid SupplierGuid { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public List<PurchaseOrderItemResponse> Items { get; set; } = new();
}
