using ProcurementService.Models;
using Shared.Events.Models;

namespace ProcurementService.Repositories.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<List<PurchaseOrder>> GetAllAsync();
    Task<PagedResult<PurchaseOrder>> GetPagedAsync(int page, int pageSize, string? search = null, string? status = null);
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<PurchaseOrder?> GetByGuidAsync(Guid guid);
    Task<PurchaseOrder?> GetByPONumberAsync(string poNumber);
    Task<string> GenerateNextPONumberAsync();
    Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder);
    Task UpdateAsync(PurchaseOrder purchaseOrder);
    Task DeleteAsync(PurchaseOrder purchaseOrder);
}
