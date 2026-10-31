using InventoryService.DTOs.Inventory;
using Shared.Events.Models;

namespace InventoryService.Services.Interfaces;

public interface IInventoryService
{
    Task<List<InventoryResponse>> GetAllAsync();
    Task<PagedResult<InventoryResponse>> GetPagedAsync(int page, int pageSize, string? search = null, string? warehouseCode = null, bool? lowStockOnly = null);
    Task<List<InventoryResponse>> GetByProductIdAsync(int productId);
    Task<List<InventoryResponse>> GetLowStockAsync();
    Task<InventoryResponse> StockInAsync(StockInRequest request);
    Task<InventoryResponse> StockOutAsync(StockOutRequest request);
    Task<List<StockTransactionResponse>> GetTransactionsAsync();
    Task<List<StockTransactionResponse>> GetTransactionsByProductIdAsync(int productId);
}
