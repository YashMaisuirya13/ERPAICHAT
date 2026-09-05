using System.ComponentModel.DataAnnotations;

namespace ProcurementService.Models;

public class PurchaseOrderItem
{
    [Key]
    public int Id { get; set; }

    public Guid PurchaseOrderItemGuid { get; set; } = Guid.NewGuid();

    public int PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid ProductGuid { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }
}
