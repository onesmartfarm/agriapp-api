namespace AgriApp.Web.Services;

public interface IInquiryService
{
    Task<List<InquiryResponse>> GetAllAsync();
    Task<InquiryResponse?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, InquiryResponse? Inquiry)> CreateAsync(CreateInquiryRequest request);
    Task<(bool Success, string? Error, InquiryResponse? Inquiry)> UpdateStatusAsync(int id, string status);
}
