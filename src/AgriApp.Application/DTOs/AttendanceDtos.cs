using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class ClockRequest
{
    [Required]
    public decimal Latitude { get; set; }

    [Required]
    public decimal Longitude { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;
}

public class AttendanceResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Type { get; set; } = string.Empty;
}
