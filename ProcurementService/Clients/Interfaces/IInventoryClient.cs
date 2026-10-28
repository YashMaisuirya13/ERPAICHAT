using ProcurementService.Clients.DTOs;

namespace ProcurementService.Clients.Interfaces;

public interface IInventoryClient
{
    Task<ProductDto?> GetProductByGuidAsync(Guid productGuid);
    Task<bool> StockInAsync(int productId, int quantity, string warehouseCode, string? referenceNumber = null);
}
