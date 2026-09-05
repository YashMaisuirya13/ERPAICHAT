namespace Shared.Events.Models;

public class POItemDto
{
    public Guid ProductGuid { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}
