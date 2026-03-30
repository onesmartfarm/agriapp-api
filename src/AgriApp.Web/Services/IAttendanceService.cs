namespace AgriApp.Web.Services;

public interface IAttendanceService
{
    Task<(bool Success, string? Error)> ClockInAsync(double? lat, double? lng);
    Task<(bool Success, string? Error)> ClockOutAsync(double? lat, double? lng);
    Task<List<AttendanceResponse>> GetMyAttendanceAsync();
}
