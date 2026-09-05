using SupplierService.DTOs.Suppliers;
using SupplierService.Models;
using SupplierService.Repositories.Interfaces;
using SupplierService.Services.Interfaces;

namespace SupplierService.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<List<SupplierResponse>> GetAllAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return suppliers.Select(MapToResponse).ToList();
    }

    public async Task<SupplierResponse?> GetByGuidAsync(Guid guid)
    {
        var supplier = await _supplierRepository.GetByGuidAsync(guid);
        return supplier == null ? null : MapToResponse(supplier);
    }

    public async Task<SupplierResponse?> GetByCodeAsync(string supplierCode)
    {
        var supplier = await _supplierRepository.GetByCodeAsync(supplierCode);
        return supplier == null ? null : MapToResponse(supplier);
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        var exists = await _supplierRepository.ExistsByCodeAsync(request.SupplierCode);
        if (exists)
        {
            throw new InvalidOperationException($"Supplier with code '{request.SupplierCode}' already exists.");
        }

        var supplier = new Supplier
        {
            SupplierCode = request.SupplierCode,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = true
        };

        var created = await _supplierRepository.CreateAsync(supplier);
        return MapToResponse(created);
    }

    public async Task<SupplierResponse?> UpdateAsync(Guid guid, UpdateSupplierRequest request)
    {
        var supplier = await _supplierRepository.GetByGuidAsync(guid);
        if (supplier == null)
        {
            return null;
        }

        supplier.Name = request.Name;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.IsActive = request.IsActive;

        await _supplierRepository.UpdateAsync(supplier);
        return MapToResponse(supplier);
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        var supplier = await _supplierRepository.GetByGuidAsync(guid);
        if (supplier == null)
        {
            return false;
        }

        await _supplierRepository.DeleteAsync(supplier);
        return true;
    }

    private static SupplierResponse MapToResponse(Supplier supplier)
    {
        return new SupplierResponse
        {
            Id = supplier.Id,
            SupplierGuid = supplier.SupplierGuid,
            SupplierCode = supplier.SupplierCode,
            Name = supplier.Name,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            IsActive = supplier.IsActive
        };
    }
}
