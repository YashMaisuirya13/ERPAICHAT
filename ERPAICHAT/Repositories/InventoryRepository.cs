using InventoryService.Data;
using InventoryService.Models;
using InventoryService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    public async Task<InventoryItem?> GetByIdAsync(int id)
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<InventoryItem?> GetByGuidAsync(Guid guid)
    {
        return await _context.InventoryItems
            .Include(x => x.Product)
            .AsNoTracking()
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
            .Include(x => x.Product)
            .Include(x => x.InventoryItem)
            .AsNoTracking()
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }

    public async Task<List<StockTransaction>> GetTransactionsByProductIdAsync(int productId)
    {
        return await _context.StockTransactions
            .Include(x => x.Product)
            .Include(x => x.InventoryItem)
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }
}
