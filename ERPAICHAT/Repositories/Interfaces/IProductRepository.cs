using InventoryService.Models;

namespace InventoryService.Repositories.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByGuidAsync(Guid guid);
    Task<Product> CreateAsync(Product product);
}
