using System.Net.Http.Json;
using System.Text.Json;
using AgriApp.Web.Models.Attendance;

namespace AgriApp.Web.HttpServices;

public class AttendanceService : IAttendanceService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public AttendanceService(HttpClient http) => _http = http;

    public async Task<AttendanceDto?> ClockInAsync(ClockRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/attendance/clock", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttendanceDto>(JsonOpts);
    }

    public async Task<AttendanceDto?> ClockOutAsync(ClockRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/attendance/clock", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttendanceDto>(JsonOpts);
    }

    public async Task<List<AttendanceDto>?> GetMyAttendanceAsync()
    {
        var response = await _http.GetAsync("api/attendance/my");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AttendanceDto>>(JsonOpts);
    }
}
