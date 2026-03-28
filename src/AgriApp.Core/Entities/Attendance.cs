using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Attendance : ICenterScoped
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public AttendanceType Type { get; set; }

    public User User { get; set; } = null!;
    public Center Center { get; set; } = null!;
}
