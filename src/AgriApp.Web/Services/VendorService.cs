using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class VendorService : IVendorService
{
    private readonly HttpClient _http;
    private readonly ILogger<VendorService> _logger;

    public VendorService(HttpClient http, ILogger<VendorService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<VendorResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<VendorResponse>>("api/vendors")
                   ?? new List<VendorResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load vendors");
            throw new ApplicationException("Could not load vendors. Please try again.", ex);
        }
    }

    public async Task<VendorResponse?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<VendorResponse>($"api/vendors/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load vendor {Id}", id);
            return null;
        }
    }

    public async Task<(bool Success, string? Error, VendorResponse? Data)> CreateAsync(CreateVendorRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/vendors", request);
            if (response.IsSuccessStatusCode)
            {
                var vendor = await response.Content.ReadFromJsonAsync<VendorResponse>();
                return (true, null, vendor);
            }
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Create vendor failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating vendor");
            return (false, "An unexpected error occurred while creating the vendor.", null);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/vendors/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting vendor {Id}", id);
            return false;
        }
    }
}
