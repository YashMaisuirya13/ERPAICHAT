namespace ProcurementService.DTOs.PurchaseOrders;

public class PurchaseOrderItemResponse
{
    public int Id { get; set; }
    public Guid PurchaseOrderItemGuid { get; set; }
    public Guid ProductGuid { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}
