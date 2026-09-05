using System.Net;
using System.Net.Http.Json;
using ProcurementService.Clients.DTOs;
using ProcurementService.Clients.Interfaces;
using ProcurementService.Exceptions;

namespace ProcurementService.Clients;

public class SupplierClient : ISupplierClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupplierClient> _logger;

    public SupplierClient(HttpClient httpClient, ILogger<SupplierClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SupplierDto?> GetSupplierByGuidAsync(Guid supplierGuid)
    {
        try
        {
            _logger.LogInformation("Calling SupplierService for SupplierGuid: {SupplierGuid}", supplierGuid);

            var response = await _httpClient.GetAsync($"/api/suppliers/{supplierGuid}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("SupplierGuid {SupplierGuid} not found in SupplierService.", supplierGuid);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SupplierService returned non-success code: {StatusCode}", response.StatusCode);
                throw new ServiceUnavailableException("SupplierService", $"SupplierService returned status code {(int)response.StatusCode} ({response.StatusCode}).");
            }

            return await response.Content.ReadFromJsonAsync<SupplierDto>();
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout occurred while connecting to SupplierService.");
            throw new ServiceUnavailableException("SupplierService", "Request to SupplierService timed out. Please verify the service is running.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while calling SupplierService.");
            throw new ServiceUnavailableException("SupplierService", "Unable to reach SupplierService. Please verify network connectivity and service status.", ex);
        }
    }
}
