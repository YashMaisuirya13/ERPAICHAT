using InventoryService.Data;
using InventoryService.Models;
using InventoryService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Events.Models;

namespace InventoryService.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _context;

    public ProductRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PagedResult<Product>> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var safePage = Math.Max(1, page);
        var safePageSize = pageSize > 0 ? pageSize : 25;

        var items = await query
            .OrderBy(p => p.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePage,
            PageSize = safePageSize
        };
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByGuidAsync(Guid guid)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.ProductGuid == guid);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }
}
