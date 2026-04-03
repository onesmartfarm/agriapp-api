namespace AgriApp.Web.Services;

public interface IPaymentService
{
    Task<(bool Success, string? Error, PaymentResultResponse? Payment)> RecordPaymentAsync(PaymentRecordRequest request);
}
