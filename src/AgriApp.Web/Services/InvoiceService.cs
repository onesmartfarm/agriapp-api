using System.Net.Http.Json;

namespace AgriApp.Web.Services;

public class InvoiceService : IInvoiceService
{
    private readonly HttpClient _http;

    public InvoiceService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<InvoiceResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<InvoiceResponse>>("api/invoices")
                   ?? new List<InvoiceResponse>();
        }
        catch
        {
            return new List<InvoiceResponse>();
        }
    }

    public async Task<InvoiceResponse?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<InvoiceResponse>($"api/invoices/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error, InvoiceResponse? Invoice)> GenerateAsync(GenerateInvoiceRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/invoices/generate", new
            {
                request.WorkOrderId,
                request.CustomerId,
                request.CenterId,
                request.DueDate,
                request.AdditionalFees
            });

            if (response.IsSuccessStatusCode)
            {
                var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
                return (true, null, invoice);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error, InvoiceResponse? Invoice)> IssueAsync(Guid id)
    {
        try
        {
            var response = await _http.PatchAsync($"api/invoices/{id}/issue", null);

            if (response.IsSuccessStatusCode)
            {
                var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
                return (true, null, invoice);
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
