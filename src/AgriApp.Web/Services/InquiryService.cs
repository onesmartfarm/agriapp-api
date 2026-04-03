using System.Net.Http.Json;

namespace AgriApp.Web.Services;

public class InquiryService : IInquiryService
{
    private readonly HttpClient _http;

    public InquiryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<InquiryResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<InquiryResponse>>("api/inquiries")
                   ?? new List<InquiryResponse>();
        }
        catch
        {
            return new List<InquiryResponse>();
        }
    }

    public async Task<InquiryResponse?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<InquiryResponse>($"api/inquiries/{id}");
        }
        catch
        {
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
            return (false, error, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
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
            return (false, error, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }
}
