namespace AgriApp.Web.Services;

public interface IVendorService
{
    Task<List<VendorResponse>> GetAllAsync();
    Task<VendorResponse?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, VendorResponse? Data)> CreateAsync(CreateVendorRequest request);
    Task<bool> DeleteAsync(int id);
}
