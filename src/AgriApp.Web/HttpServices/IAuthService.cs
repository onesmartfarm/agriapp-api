using AgriApp.Web.Models;

namespace AgriApp.Web.HttpServices;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task LogoutAsync();
}
