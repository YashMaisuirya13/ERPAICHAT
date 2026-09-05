using ProcurementService.Clients.DTOs;

namespace ProcurementService.Clients.Interfaces;

public interface IInventoryClient
{
    Task<ProductDto?> GetProductByGuidAsync(Guid productGuid);
}
