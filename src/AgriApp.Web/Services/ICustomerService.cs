namespace AgriApp.Web.Services;

public interface ICustomerService
{
    Task<List<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<CustomerFinancialSummaryResponse?> GetSummaryAsync(int customerId);
    Task<List<InquiryResponse>> GetInquiriesForCustomerAsync(int customerId);
    Task<List<WorkOrderListItem>> GetWorkOrdersForCustomerAsync(int customerId);
    Task<List<CustomerLedgerEntryResponse>> GetLedgerAsync(int customerId);
    Task<(bool Success, string? Error, CustomerResponse? Data)> CreateAsync(CreateCustomerRequest request);
    Task<(bool Success, string? Error, CustomerResponse? Data)> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(int id);
}
