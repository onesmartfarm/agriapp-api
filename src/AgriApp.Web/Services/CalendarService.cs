using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

/// <summary>
/// Client wrapper for the Calendar API (/api/calendar/capacity).
/// Fetches equipment capacity and work order density for scheduling views.
/// </summary>
public class CalendarService : ICalendarService
{
    private readonly HttpClient _http;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(HttpClient http, ILogger<CalendarService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Get center capacity (work order density) for a date range.
    /// API calls GET /api/calendar/capacity?start={startDate}&end={endDate}
    /// </summary>
    public async Task<List<CapacityScheduleItem>> GetCenterCapacityAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");

            if ((endDate - startDate).TotalDays > 366)
                throw new ArgumentException("Date range cannot exceed 366 days.");

            // Format dates as ISO 8601 UTC
            var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var result = await _http.GetFromJsonAsync<List<CapacityScheduleItem>>(
                $"api/calendar/capacity?start={Uri.EscapeDataString(startStr)}&end={Uri.EscapeDataString(endStr)}")
                ?? new List<CapacityScheduleItem>();

            _logger.LogInformation("Loaded {Count} capacity items for range {Start}-{End}", 
                result.Count, startDate.Date, endDate.Date);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load calendar capacity for range {Start}-{End}", startDate, endDate);
            throw new ApplicationException("Could not load capacity data. Please check your connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading calendar capacity");
            throw new ApplicationException("An unexpected error occurred while loading calendar data.", ex);
        }
    }
}
