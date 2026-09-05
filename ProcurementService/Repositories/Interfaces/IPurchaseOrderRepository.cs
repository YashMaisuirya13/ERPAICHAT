using ProcurementService.Models;

namespace ProcurementService.Repositories.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<List<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<PurchaseOrder?> GetByGuidAsync(Guid guid);
    Task<PurchaseOrder?> GetByPONumberAsync(string poNumber);
    Task<string> GenerateNextPONumberAsync();
    Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder);
    Task UpdateAsync(PurchaseOrder purchaseOrder);
    Task DeleteAsync(PurchaseOrder purchaseOrder);
}
