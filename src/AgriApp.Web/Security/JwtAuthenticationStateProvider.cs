using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AgriApp.Web.Security;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "agriapp_jwt";
    private readonly ILocalStorageService _localStorage;
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync(TokenKey);
            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            var claims = ParseClaims(token);
            if (IsExpired(claims))
            {
                await _localStorage.RemoveItemAsync(TokenKey);
                return Anonymous;
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _localStorage.SetItemAsStringAsync(TokenKey, token);
        var claims = ParseClaims(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public async Task<string?> GetTokenAsync()
        => await _localStorage.GetItemAsStringAsync(TokenKey);

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return Enumerable.Empty<Claim>();

        var payload = parts[1];
        // Add base64 padding
        var rem = payload.Length % 4;
        if (rem == 2) payload += "==";
        else if (rem == 3) payload += "=";
        payload = payload.Replace('-', '+').Replace('_', '/');

        var bytes = Convert.FromBase64String(payload);
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                   ?? new Dictionary<string, JsonElement>();

        var claims = new List<Claim>();
        foreach (var (key, value) in dict)
        {
            var claimType = key switch
            {
                "role" or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    => ClaimTypes.Role,
                "email" or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
                    => ClaimTypes.Email,
                "sub" or "nameid" or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
                    => ClaimTypes.NameIdentifier,
                "name" or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                    => ClaimTypes.Name,
                _ => key
            };

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    claims.Add(new Claim(claimType, item.GetString() ?? ""));
            }
            else
            {
                claims.Add(new Claim(claimType, value.ToString()));
            }
        }
        return claims;
    }

    private static bool IsExpired(IEnumerable<Claim> claims)
    {
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim == null) return false;
        if (!long.TryParse(expClaim.Value, out var exp)) return false;
        var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
        return expiry <= DateTimeOffset.UtcNow;
    }
}
