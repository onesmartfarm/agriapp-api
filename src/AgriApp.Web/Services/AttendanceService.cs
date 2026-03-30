using System.Net.Http.Json;

namespace AgriApp.Web.Services;

public class AttendanceService : IAttendanceService
{
    private readonly HttpClient _http;

    public AttendanceService(HttpClient http)
    {
        _http = http;
    }

    public Task<(bool Success, string? Error)> ClockInAsync(double? lat, double? lng)
        => PostClockAsync("ClockIn", lat, lng);

    public Task<(bool Success, string? Error)> ClockOutAsync(double? lat, double? lng)
        => PostClockAsync("ClockOut", lat, lng);

    public async Task<List<AttendanceResponse>> GetMyAttendanceAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AttendanceResponse>>("api/attendance/my")
                   ?? new List<AttendanceResponse>();
        }
        catch
        {
            return new List<AttendanceResponse>();
        }
    }

    private async Task<(bool Success, string? Error)> PostClockAsync(
        string action, double? lat, double? lng)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/attendance/clock",
                new AttendanceClockRequest(action, lat, lng));

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
