namespace AgriApp.Web.Services;

public interface ICustomerService
{
    Task<List<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, CustomerResponse? Data)> CreateAsync(CreateCustomerRequest request);
    Task<(bool Success, string? Error, CustomerResponse? Data)> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(int id);
}
