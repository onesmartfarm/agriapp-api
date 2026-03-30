using System.Security.Claims;
using System.Text.Json;
using AgriApp.Core.Enums;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AgriApp.Web.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "agriapp_token";

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

            var claims = ParseClaimsFromJwt(token);
            if (!claims.Any())
                return Anonymous;

            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (expClaim != null && long.TryParse(expClaim, out var exp))
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                if (expiry < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync(TokenKey);
                    return Anonymous;
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    public async Task NotifyUserLoginAsync(string token)
    {
        await _localStorage.SetItemAsStringAsync(TokenKey, token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task NotifyUserLogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public async Task<string?> GetTokenAsync()
        => await _localStorage.GetItemAsStringAsync(TokenKey);

    public async Task<UserClaims?> GetUserClaimsAsync()
    {
        var state = await GetAuthenticationStateAsync();
        var user = state.User;
        if (user.Identity?.IsAuthenticated != true)
            return null;

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value
                     ?? user.FindFirst("role")?.Value;
        var centerIdClaim = user.FindFirst("centerId")?.Value
                         ?? user.FindFirst("CenterId")?.Value;
        var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value
                      ?? user.FindFirst("email")?.Value;
        var nameClaim = user.FindFirst(ClaimTypes.Name)?.Value
                     ?? user.FindFirst("name")?.Value
                     ?? emailClaim;

        Enum.TryParse<Role>(roleClaim, out var role);
        int.TryParse(centerIdClaim, out var centerId);

        return new UserClaims
        {
            Email = emailClaim ?? string.Empty,
            Name = nameClaim ?? string.Empty,
            Role = role,
            CenterId = centerIdClaim != null ? centerId : (int?)null
        };
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return claims;

            var payload = parts[1];
            // Pad base64url to standard base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (keyValuePairs == null) return claims;

            foreach (var (key, value) in keyValuePairs)
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in value.EnumerateArray())
                        claims.Add(new Claim(key, item.GetString() ?? string.Empty));
                }
                else
                {
                    claims.Add(new Claim(key, value.ToString()));
                }
            }

            NormalizeClaims(claims);
        }
        catch
        {
            // Invalid JWT — return empty
        }
        return claims;
    }

    private static void NormalizeClaims(List<Claim> claims)
    {
        // Map common JWT claim names to .NET ClaimTypes
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "email", ClaimTypes.Email },
            { "sub",   ClaimTypes.NameIdentifier },
            { "name",  ClaimTypes.Name }
        };

        var toAdd = new List<Claim>();
        foreach (var claim in claims)
        {
            if (mappings.TryGetValue(claim.Type, out var mapped)
                && !claims.Any(c => c.Type == mapped))
            {
                toAdd.Add(new Claim(mapped, claim.Value));
            }
        }
        claims.AddRange(toAdd);
    }
}

public sealed class UserClaims
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Role Role { get; set; }
    public int? CenterId { get; set; }
}
