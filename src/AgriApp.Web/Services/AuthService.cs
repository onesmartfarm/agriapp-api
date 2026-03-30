using System.Net.Http.Json;
using AgriApp.Web.Security;

namespace AgriApp.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly JwtAuthenticationStateProvider _authProvider;

    public AuthService(HttpClient http, JwtAuthenticationStateProvider authProvider)
    {
        _http = http;
        _authProvider = authProvider;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login",
                new LoginRequest(email, password));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(error) ? "Login failed." : error);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token == null)
                return (false, "No token received.");

            await _authProvider.MarkUserAsAuthenticated(result.Token);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
        => await _authProvider.MarkUserAsLoggedOut();
}
