using System.ComponentModel.DataAnnotations;

namespace ProcurementService.DTOs.PurchaseOrders;

public class CreatePurchaseOrderItemRequest
{
    [Required]
    public Guid ProductGuid { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
