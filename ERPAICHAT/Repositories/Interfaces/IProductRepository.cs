using InventoryService.Models;
using Shared.Events.Models;

namespace InventoryService.Repositories.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<PagedResult<Product>> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByGuidAsync(Guid guid);
    Task<Product> CreateAsync(Product product);
}
