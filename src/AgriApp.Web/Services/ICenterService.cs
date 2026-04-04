namespace AgriApp.Web.Services;

public interface ICenterService
{
    Task<List<CenterResponse>> GetAllAsync();
    Task<CenterResponse?> GetByIdAsync(int id);
}
