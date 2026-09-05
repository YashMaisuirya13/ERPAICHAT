using Microsoft.EntityFrameworkCore;
using SupplierService.Data;
using SupplierService.Models;
using SupplierService.Repositories.Interfaces;

namespace SupplierService.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly SupplierDbContext _context;

    public SupplierRepository(SupplierDbContext context)
    {
        _context = context;
    }

    public async Task<List<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier?> GetByGuidAsync(Guid guid)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierGuid == guid);
    }

    public async Task<Supplier?> GetByCodeAsync(string supplierCode)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierCode == supplierCode);
    }

    public async Task<bool> ExistsByCodeAsync(string supplierCode)
    {
        return await _context.Suppliers
            .AnyAsync(s => s.SupplierCode == supplierCode);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Supplier supplier)
    {
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
    }
}
