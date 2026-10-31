using InventoryService.DTOs.Products;
using InventoryService.Models;
using InventoryService.Repositories.Interfaces;
using InventoryService.Services.Interfaces;
using Shared.Events.Models;

namespace InventoryService.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<ProductResponse>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(MapToResponse).ToList();
    }

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var pagedProducts = await _productRepository.GetPagedAsync(page, pageSize, search);
        return new PagedResult<ProductResponse>
        {
            Items = pagedProducts.Items.Select(MapToResponse).ToList(),
            TotalCount = pagedProducts.TotalCount,
            PageNumber = pagedProducts.PageNumber,
            PageSize = pagedProducts.PageSize
        };
    }

    public async Task<ProductResponse?> GetByGuidAsync(Guid guid)
    {
        var product = await _productRepository.GetByGuidAsync(guid);
        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            SKU = request.SKU,
            Name = request.Name,
            UnitPrice = request.UnitPrice,
            MinimumStock = request.MinimumStock
        };

        var createdProduct = await _productRepository.CreateAsync(product);
        return MapToResponse(createdProduct);
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            ProductGuid = product.ProductGuid,
            SKU = product.SKU,
            Name = product.Name,
            UnitPrice = product.UnitPrice,
            MinimumStock = product.MinimumStock
        };
    }
}
