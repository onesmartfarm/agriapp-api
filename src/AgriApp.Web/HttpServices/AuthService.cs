using System.Net.Http.Json;
using System.Text.Json;
using AgriApp.Web.Auth;
using AgriApp.Web.Models;

namespace AgriApp.Web.HttpServices;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, JwtAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Token != null)
            await _authStateProvider.NotifyUserLoginAsync(result.Token);

        return result;
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.NotifyUserLogoutAsync();
    }
}
