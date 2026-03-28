using System.Security.Claims;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Api.Middleware;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int UserId => int.TryParse(
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public Role Role => Enum.TryParse<Role>(
        User?.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : Role.Staff;

    public int? CenterId
    {
        get
        {
            var claim = User?.FindFirst("CenterId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
