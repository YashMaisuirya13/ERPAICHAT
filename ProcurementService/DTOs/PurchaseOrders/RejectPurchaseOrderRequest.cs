using System.ComponentModel.DataAnnotations;

namespace ProcurementService.DTOs.PurchaseOrders;

public class RejectPurchaseOrderRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
