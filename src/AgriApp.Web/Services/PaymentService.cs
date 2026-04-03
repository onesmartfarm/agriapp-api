using System.Net.Http.Json;

namespace AgriApp.Web.Services;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _http;

    public PaymentService(HttpClient http)
    {
        _http = http;
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
            return (false, error, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }
}
