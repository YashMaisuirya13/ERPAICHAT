using ProcurementService.Clients.DTOs;

namespace ProcurementService.Clients.Interfaces;

public interface ISupplierClient
{
    Task<SupplierDto?> GetSupplierByGuidAsync(Guid supplierGuid);
}
