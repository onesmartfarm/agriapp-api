using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _http;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(HttpClient http, ILogger<PaymentService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, PaymentResultResponse? Payment)> RecordPaymentAsync(PaymentRecordRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/payments", new
            {
                InvoiceId = request.InvoiceId,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                TransactionReference = request.TransactionReference
            });

            if (response.IsSuccessStatusCode)
            {
                var payment = await response.Content.ReadFromJsonAsync<PaymentResultResponse>();
                return (true, null, payment);
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Record payment failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception recording payment for invoice {InvoiceId}", request.InvoiceId);
            return (false, "An unexpected error occurred while recording the payment.", null);
        }
    }
}
