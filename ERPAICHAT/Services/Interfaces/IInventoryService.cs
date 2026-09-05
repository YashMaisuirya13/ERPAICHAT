using InventoryService.DTOs.Inventory;

namespace InventoryService.Services.Interfaces;

public interface IInventoryService
{
    Task<List<InventoryResponse>> GetAllAsync();
    Task<List<InventoryResponse>> GetByProductIdAsync(int productId);
    Task<List<InventoryResponse>> GetLowStockAsync();
    Task<InventoryResponse> StockInAsync(StockInRequest request);
    Task<InventoryResponse> StockOutAsync(StockOutRequest request);
    Task<List<StockTransactionResponse>> GetTransactionsAsync();
    Task<List<StockTransactionResponse>> GetTransactionsByProductIdAsync(int productId);
}
