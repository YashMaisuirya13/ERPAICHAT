using SupplierService.DTOs.Suppliers;

namespace SupplierService.Services.Interfaces;

public interface ISupplierService
{
    Task<List<SupplierResponse>> GetAllAsync();
    Task<SupplierResponse?> GetByGuidAsync(Guid guid);
    Task<SupplierResponse?> GetByCodeAsync(string supplierCode);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request);
    Task<SupplierResponse?> UpdateAsync(Guid guid, UpdateSupplierRequest request);
    Task<bool> DeleteAsync(Guid guid);
}
