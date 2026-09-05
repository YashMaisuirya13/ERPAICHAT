using System.ComponentModel.DataAnnotations;

namespace ProcurementService.Models;

public class PurchaseOrder
{
    [Key]
    public int Id { get; set; }

    public Guid PurchaseOrderGuid { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string PONumber { get; set; } = string.Empty;

    public Guid SupplierGuid { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";

    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
