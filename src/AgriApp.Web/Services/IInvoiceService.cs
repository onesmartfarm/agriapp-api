namespace AgriApp.Web.Services;

public interface IInvoiceService
{
    Task<List<InvoiceResponse>> GetAllAsync();
    Task<InvoiceResponse?> GetByIdAsync(Guid id);
    Task<(bool Success, string? Error, InvoiceResponse? Invoice)> GenerateAsync(GenerateInvoiceRequest request);
    Task<(bool Success, string? Error, InvoiceResponse? Invoice)> IssueAsync(Guid id);
}
