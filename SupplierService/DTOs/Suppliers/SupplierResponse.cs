namespace SupplierService.DTOs.Suppliers;

public class SupplierResponse
{
    public int Id { get; set; }
    public Guid SupplierGuid { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
