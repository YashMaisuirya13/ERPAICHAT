namespace ProcurementService.Clients.DTOs;

public class SupplierDto
{
    public Guid SupplierGuid { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
