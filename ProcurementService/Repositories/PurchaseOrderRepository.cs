using Microsoft.EntityFrameworkCore;
using ProcurementService.Data;
using ProcurementService.Models;
using ProcurementService.Repositories.Interfaces;
using Shared.Events.Models;

namespace ProcurementService.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ProcurementDbContext _context;

    public PurchaseOrderRepository(ProcurementDbContext context)
    {
        _context = context;
    }

    public async Task<List<PurchaseOrder>> GetAllAsync()
    {
        return await _context.PurchaseOrders
            .Include(x => x.Items)
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync();
    }

    public async Task<PagedResult<PurchaseOrder>> GetPagedAsync(int page, int pageSize, string? search = null, string? status = null)
    {
        var query = _context.PurchaseOrders
            .Include(x => x.Items)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status.ToLower() == status.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.PONumber.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();
        var safePage = Math.Max(1, page);
        var safePageSize = pageSize > 0 ? pageSize : 25;

        var items = await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return new PagedResult<PurchaseOrder>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePage,
            PageSize = safePageSize
        };
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PurchaseOrder?> GetByGuidAsync(Guid guid)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.PurchaseOrderGuid == guid);
    }

    public async Task<PurchaseOrder?> GetByPONumberAsync(string poNumber)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.PONumber == poNumber);
    }

    public async Task<string> GenerateNextPONumberAsync()
    {
        var count = await _context.PurchaseOrders.CountAsync();
        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();
        return purchaseOrder;
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Remove(purchaseOrder);
        await _context.SaveChangesAsync();
    }
}
