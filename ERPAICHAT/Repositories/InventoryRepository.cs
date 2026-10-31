using InventoryService.Data;
using InventoryService.Models;
using InventoryService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Events.Models;

namespace InventoryService.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;

    public InventoryRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItem>> GetAllAsync()
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PagedResult<InventoryItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? warehouseCode = null,
        bool? lowStockOnly = null)
    {
        var query = _context.InventoryItems
            .Include(x => x.Product)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(warehouseCode))
        {
            query = query.Where(x => x.WarehouseCode == warehouseCode);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Product.Name.ToLower().Contains(term) ||
                x.Product.SKU.ToLower().Contains(term) ||
                x.WarehouseCode.ToLower().Contains(term));
        }

        if (lowStockOnly == true)
        {
            query = query.Where(x => x.Quantity <= x.Product.MinimumStock);
        }

        var totalCount = await query.CountAsync();
        var safePage = Math.Max(1, page);
        var safePageSize = pageSize > 0 ? pageSize : 25;

        var items = await query
            .OrderBy(x => x.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return new PagedResult<InventoryItem>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePage,
            PageSize = safePageSize
        };
    }

    public async Task<InventoryItem?> GetByIdAsync(int id)
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<InventoryItem?> GetByGuidAsync(Guid guid)
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.InventoryGuid == guid);
    }

    public async Task<InventoryItem?> GetByProductAndWarehouseAsync(int productId, string warehouseCode)
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseCode == warehouseCode);
    }

    public async Task<List<InventoryItem>> GetByProductIdAsync(int productId)
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync();
    }

    public async Task<List<InventoryItem>> GetLowStockAsync()
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .AsNoTracking()
            .Where(x => x.Quantity <= x.Product.MinimumStock)
            .ToListAsync();
    }

    public async Task<InventoryItem> CreateAsync(InventoryItem inventoryItem)
    {
        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();
        return inventoryItem;
    }

    public async Task UpdateAsync(InventoryItem inventoryItem)
    {
        _context.InventoryItems.Update(inventoryItem);
        await _context.SaveChangesAsync();
    }

    public async Task<StockTransaction> AddTransactionAsync(StockTransaction transaction)
    {
        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<List<StockTransaction>> GetTransactionsAsync()
    {
        return await _context.StockTransactions
            .AsNoTracking()
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }

    public async Task<List<StockTransaction>> GetTransactionsByProductIdAsync(int productId)
    {
        return await _context.StockTransactions
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }
}
