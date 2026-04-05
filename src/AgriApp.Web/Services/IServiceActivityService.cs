namespace AgriApp.Web.Services;

public interface IServiceActivityService
{
    Task<List<ServiceActivityRow>> GetAllAsync(int? centerId = null);
    Task<ServiceActivityRow?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, ServiceActivityRow? Row)> CreateAsync(CreateServiceActivityForm request);
    Task<(bool Success, string? Error, ServiceActivityRow? Row)> UpdateAsync(int id, UpdateServiceActivityForm request);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
