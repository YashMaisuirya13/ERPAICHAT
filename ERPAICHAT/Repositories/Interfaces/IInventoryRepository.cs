using InventoryService.Models;
using Shared.Events.Models;

namespace InventoryService.Repositories.Interfaces;

public interface IInventoryRepository
{
    Task<List<InventoryItem>> GetAllAsync();
    Task<PagedResult<InventoryItem>> GetPagedAsync(int page, int pageSize, string? search = null, string? warehouseCode = null, bool? lowStockOnly = null);
    Task<InventoryItem?> GetByIdAsync(int id);
    Task<InventoryItem?> GetByGuidAsync(Guid guid);
    Task<InventoryItem?> GetByProductAndWarehouseAsync(int productId, string warehouseCode);
    Task<List<InventoryItem>> GetByProductIdAsync(int productId);
    Task<List<InventoryItem>> GetLowStockAsync();
    Task<InventoryItem> CreateAsync(InventoryItem inventoryItem);
    Task UpdateAsync(InventoryItem inventoryItem);

    Task<StockTransaction> AddTransactionAsync(StockTransaction transaction);
    Task<List<StockTransaction>> GetTransactionsAsync();
    Task<List<StockTransaction>> GetTransactionsByProductIdAsync(int productId);
}
