using InventoryService.Data;
using InventoryService.Models;
using InventoryService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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
