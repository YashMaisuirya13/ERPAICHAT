using InventoryService.DTOs.Products;
using Shared.Events.Models;

namespace InventoryService.Services.Interfaces;

public interface IProductService
{
    Task<List<ProductResponse>> GetAllAsync();
    Task<PagedResult<ProductResponse>> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<ProductResponse?> GetByGuidAsync(Guid guid);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
}
