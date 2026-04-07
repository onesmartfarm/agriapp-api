using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class InquiryService : IInquiryService
{
    private readonly HttpClient _http;
    private readonly ILogger<InquiryService> _logger;

    public InquiryService(HttpClient http, ILogger<InquiryService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<InquiryResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<InquiryResponse>>("api/inquiries")
                   ?? new List<InquiryResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inquiries");
            throw new ApplicationException("Could not load inquiries. Please try again.", ex);
        }
    }

    public async Task<InquiryResponse?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<InquiryResponse>($"api/inquiries/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inquiry {Id}", id);
            return null;
        }
    }

    public async Task<(bool Success, string? Error, InquiryResponse? Inquiry)> CreateAsync(CreateInquiryRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/inquiries", new
            {
                request.CustomerId,
                request.ServiceActivityId,
                request.EquipmentId,
                request.SalespersonId,
                request.CenterId
            });

            if (response.IsSuccessStatusCode)
            {
                var inquiry = await response.Content.ReadFromJsonAsync<InquiryResponse>();
                return (true, null, inquiry);
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Create inquiry failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating inquiry");
            return (false, "An unexpected error occurred while creating the inquiry.", null);
        }
    }

    public async Task<(bool Success, string? Error, InquiryResponse? Inquiry)> UpdateStatusAsync(int id, string status)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/inquiries/{id}/status", new { Status = status });

            if (response.IsSuccessStatusCode)
            {
                var inquiry = await response.Content.ReadFromJsonAsync<InquiryResponse>();
                return (true, null, inquiry);
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Update inquiry {Id} status failed: {Error}", id, error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating inquiry {Id} status", id);
            return (false, "An unexpected error occurred while updating the inquiry.", null);
        }
    }
}
