using System.Net;
using System.Net.Http.Json;
using ProcurementService.Clients.DTOs;
using ProcurementService.Clients.Interfaces;
using ProcurementService.Exceptions;

namespace ProcurementService.Clients;

public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryClient> _logger;

    public InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductByGuidAsync(Guid productGuid)
    {
        try
        {
            _logger.LogInformation("Calling InventoryService for ProductGuid: {ProductGuid}", productGuid);

            var response = await _httpClient.GetAsync($"/api/products/{productGuid}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("ProductGuid {ProductGuid} not found in InventoryService.", productGuid);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("InventoryService returned non-success code: {StatusCode}", response.StatusCode);
                throw new ServiceUnavailableException("InventoryService", $"InventoryService returned status code {(int)response.StatusCode} ({response.StatusCode}).");
            }

            return await response.Content.ReadFromJsonAsync<ProductDto>();
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout occurred while connecting to InventoryService.");
            throw new ServiceUnavailableException("InventoryService", "Request to InventoryService timed out. Please verify the service is running.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while calling InventoryService.");
            throw new ServiceUnavailableException("InventoryService", "Unable to reach InventoryService. Please verify network connectivity and service status.", ex);
        }
    }

    public async Task<bool> StockInAsync(int productId, int quantity, string warehouseCode, string? referenceNumber = null)
    {
        try
        {
            _logger.LogInformation("Calling InventoryService StockIn for ProductId: {ProductId}, Qty: {Qty}, WH: {WH}", productId, quantity, warehouseCode);

            var payload = new
            {
                productId = productId,
                quantity = quantity,
                warehouseCode = warehouseCode,
                referenceNumber = referenceNumber
            };

            var response = await _httpClient.PostAsJsonAsync("/api/inventory/stock-in", payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("InventoryService StockIn returned non-success code {StatusCode}: {Body}", response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call InventoryService StockIn for ProductId: {ProductId}", productId);
            return false;
        }
    }
}
