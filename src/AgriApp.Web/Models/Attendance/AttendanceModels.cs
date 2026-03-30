using System.ComponentModel.DataAnnotations;

namespace AgriApp.Web.Models.Attendance;

public class AttendanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AttendanceType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class ClockRequest
{
    [Required]
    public string AttendanceType { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
