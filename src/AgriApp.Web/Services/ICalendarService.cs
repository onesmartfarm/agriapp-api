namespace AgriApp.Web.Services;

/// <summary>
/// Service for fetching equipment capacity and schedule data.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Get center capacity for the given date range (read-only, AsNoTracking).
    /// </summary>
    Task<List<CapacityScheduleItem>> GetCenterCapacityAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// GET /api/calendar/timeline — equipment swimlanes with work, travel buffers, and breakdown logs.
    /// </summary>
    Task<EquipmentTimelineEnvelopeDto?> GetEquipmentTimelineAsync(DateTime rangeStartUtc, DateTime rangeEndUtc);
}

/// <summary>
/// Represents a single day's capacity for equipment scheduling.
/// </summary>
public record CapacityScheduleItem(
    DateTime Date,
    string? EquipmentName,
    int WorkOrderCount,
    double TotalDurationHours,
    double? UtilizationPercentage);
