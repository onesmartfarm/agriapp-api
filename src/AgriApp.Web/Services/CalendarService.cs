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

            var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url =
                $"api/calendar/capacity?start={Uri.EscapeDataString(startStr)}&end={Uri.EscapeDataString(endStr)}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Calendar capacity request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<CenterCapacityApiResponse>();
            var result = MapCapacityResponseToScheduleItems(payload);

            _logger.LogInformation(
                "Loaded {Count} aggregated capacity rows for range {Start}-{End} ({WorkOrders} work orders)",
                result.Count,
                startDate.Date,
                endDate.Date,
                payload?.TotalScheduled ?? 0);

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

    private static List<CapacityScheduleItem> MapCapacityResponseToScheduleItems(CenterCapacityApiResponse? api)
    {
        var list = new List<CapacityScheduleItem>();
        if (api?.WorkOrders is not { Count: > 0 })
            return list;

        var groups = new Dictionary<(DateTime Day, string Label), (HashSet<int> WorkOrderIds, double Hours)>();

        foreach (var slot in api.WorkOrders)
        {
            var start = AsUtc(slot.ScheduledStartDate);
            var end = AsUtc(slot.ScheduledEndDate);
            if (end <= start)
                continue;

            var label = BuildEquipmentLabel(slot);

            for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            {
                var calDayEndExclusive = day.AddDays(1);
                var segStart = start > day ? start : day;
                var segEnd = end < calDayEndExclusive ? end : calDayEndExclusive;
                if (segStart >= segEnd)
                    continue;

                var hours = (segEnd - segStart).TotalHours;
                var key = (day, label);
                if (!groups.TryGetValue(key, out var agg))
                {
                    agg = (new HashSet<int>(), 0.0);
                    groups[key] = agg;
                }

                agg.WorkOrderIds.Add(slot.WorkOrderId);
                groups[key] = (agg.WorkOrderIds, agg.Hours + hours);
            }
        }

        foreach (var ((day, label), (woIds, hours)) in groups)
        {
            list.Add(new CapacityScheduleItem(
                Date: day,
                EquipmentName: label,
                WorkOrderCount: woIds.Count,
                TotalDurationHours: hours,
                UtilizationPercentage: null));
        }

        return list.OrderBy(x => x.Date).ThenBy(x => x.EquipmentName).ToList();
    }

    private static DateTime AsUtc(DateTime dt) =>
        dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };

    private static string BuildEquipmentLabel(CapacitySlotApiDto slot)
    {
        if (slot.ImplementId is > 0)
            return $"Implement #{slot.ImplementId}";
        if (slot.TractorId is > 0)
            return $"Tractor #{slot.TractorId}";
        if (!string.IsNullOrWhiteSpace(slot.Description))
        {
            var d = slot.Description.Trim();
            return d.Length > 48 ? string.Concat(d.AsSpan(0, 48), "…") : d;
        }

        return $"Work order #{slot.WorkOrderId}";
    }
}
