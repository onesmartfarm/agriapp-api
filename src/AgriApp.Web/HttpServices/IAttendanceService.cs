using AgriApp.Web.Models.Attendance;

namespace AgriApp.Web.HttpServices;

public interface IAttendanceService
{
    Task<AttendanceDto?> ClockInAsync(ClockRequest request);
    Task<AttendanceDto?> ClockOutAsync(ClockRequest request);
    Task<List<AttendanceDto>?> GetMyAttendanceAsync();
}
