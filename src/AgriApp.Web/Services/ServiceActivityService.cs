using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class ServiceActivityService : IServiceActivityService
{
    private readonly HttpClient _http;
    private readonly ILogger<ServiceActivityService> _logger;

    public ServiceActivityService(HttpClient http, ILogger<ServiceActivityService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ServiceActivityRow>> GetAllAsync(int? centerId = null)
    {
        try
        {
            var url = centerId is int c
                ? $"api/service-activities?centerId={c}"
                : "api/service-activities";
            return await _http.GetFromJsonAsync<List<ServiceActivityRow>>(url)
                   ?? new List<ServiceActivityRow>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load service activities");
            throw new ApplicationException("Could not load service activities.", ex);
        }
    }

    public async Task<ServiceActivityRow?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ServiceActivityRow>($"api/service-activities/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load service activity {Id}", id);
            return null;
        }
    }

    public async Task<(bool Success, string? Error, ServiceActivityRow? Row)> CreateAsync(CreateServiceActivityForm request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/service-activities", new
            {
                request.Name,
                request.Description,
                request.BaseRatePerHour,
                request.CenterId
            });

            if (response.IsSuccessStatusCode)
            {
                var row = await response.Content.ReadFromJsonAsync<ServiceActivityRow>();
                return (true, null, row);
            }

            return (false, await response.Content.ReadAsStringAsync(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create service activity failed");
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error, ServiceActivityRow? Row)> UpdateAsync(int id, UpdateServiceActivityForm request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/service-activities/{id}", new
            {
                request.Name,
                request.Description,
                request.BaseRatePerHour
            });

            if (response.IsSuccessStatusCode)
            {
                var row = await response.Content.ReadFromJsonAsync<ServiceActivityRow>();
                return (true, null, row);
            }

            return (false, await response.Content.ReadAsStringAsync(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update service activity failed");
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/service-activities/{id}");
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete service activity failed");
            return (false, ex.Message);
        }
    }
}
