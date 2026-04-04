using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class UserService : IUserService
{
    private readonly HttpClient _http;
    private readonly ILogger<UserService> _logger;

    public UserService(HttpClient http, ILogger<UserService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserResponse>>("api/users")
                   ?? new List<UserResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users");
            throw new ApplicationException("Could not load user list. Please try again.", ex);
        }
    }

    public async Task<UserResponse?> GetMeAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<UserResponse>("api/users/me");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current user");
            return null;
        }
    }
}
