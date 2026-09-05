using System.ComponentModel.DataAnnotations;

namespace ProcurementService.DTOs.PurchaseOrders;

public class CreatePurchaseOrderRequest
{
    [Required]
    public Guid SupplierGuid { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Purchase order must contain at least one item.")]
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
}
