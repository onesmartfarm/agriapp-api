namespace AgriApp.Web.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(string email, string password);
    Task LogoutAsync();
}
