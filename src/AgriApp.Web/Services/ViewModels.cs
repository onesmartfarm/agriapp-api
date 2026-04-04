using AgriApp.Core.Enums;

namespace AgriApp.Web.Services;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Role, int? CenterId);

public record WorkOrderListItem(
    int Id,
    string Description,
    WorkStatus Status,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    string? EquipmentName,
    decimal TotalMaterialCost);

public record AttendanceClockRequest(
    string Action,
    double? Latitude,
    double? Longitude);

public record AttendanceResponse(
    int Id,
    string Action,
    DateTime Timestamp,
    double? Latitude,
    double? Longitude);
