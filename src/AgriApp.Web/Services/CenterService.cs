using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class CenterService : ICenterService
{
    private readonly HttpClient _http;
    private readonly ILogger<CenterService> _logger;

    public CenterService(HttpClient http, ILogger<CenterService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<CenterResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CenterResponse>>("api/centers")
                   ?? new List<CenterResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load centers");
            throw new ApplicationException("Could not load centers. Please try again.", ex);
        }
    }

    public async Task<CenterResponse?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<CenterResponse>($"api/centers/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load center {Id}", id);
            return null;
        }
    }
}
