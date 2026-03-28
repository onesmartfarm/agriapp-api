using AgriApp.Core.Enums;

namespace AgriApp.Core.Interfaces;

public interface ICurrentUser
{
    int UserId { get; }
    string Email { get; }
    Role Role { get; }
    int? CenterId { get; }
}
