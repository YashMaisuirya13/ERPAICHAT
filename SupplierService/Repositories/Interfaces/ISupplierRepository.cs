using SupplierService.Models;

namespace SupplierService.Repositories.Interfaces;

public interface ISupplierRepository
{
    Task<List<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<Supplier?> GetByGuidAsync(Guid guid);
    Task<Supplier?> GetByCodeAsync(string supplierCode);
    Task<bool> ExistsByCodeAsync(string supplierCode);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
}
