using InventoryService.DTOs.Products;

namespace InventoryService.Services.Interfaces;

public interface IProductService
{
    Task<List<ProductResponse>> GetAllAsync();
    Task<ProductResponse?> GetByGuidAsync(Guid guid);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
}
