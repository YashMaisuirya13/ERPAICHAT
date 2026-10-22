namespace AIAgentService.Tools.DTOs;

public class PurchaseOrderResultDto
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
    public List<PurchaseOrderItemResultDto> Items { get; set; } = new();
}

public class PurchaseOrderItemResultDto
{
    public Guid ProductGuid { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}
