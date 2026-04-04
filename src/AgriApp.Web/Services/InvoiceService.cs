using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class InvoiceService : IInvoiceService
{
    private readonly HttpClient _http;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(HttpClient http, ILogger<InvoiceService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<InvoiceResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<InvoiceResponse>>("api/invoices")
                   ?? new List<InvoiceResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load invoices");
            throw new ApplicationException("Could not load invoices. Please try again.", ex);
        }
    }

    public async Task<InvoiceResponse?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<InvoiceResponse>($"api/invoices/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load invoice {Id}", id);
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
            _logger.LogWarning("Generate invoice failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception generating invoice");
            return (false, "An unexpected error occurred while generating the invoice.", null);
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
            _logger.LogWarning("Issue invoice {Id} failed: {Error}", id, error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception issuing invoice {Id}", id);
            return (false, "An unexpected error occurred while issuing the invoice.", null);
        }
    }
}
