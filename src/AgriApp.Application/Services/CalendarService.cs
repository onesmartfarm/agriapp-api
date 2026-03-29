using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class CapacitySlot
{
    public int WorkOrderId { get; set; }
    public int CenterId { get; set; }
    public int? EquipmentId { get; set; }
    public int ResponsibleUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
}

public class CenterCapacityResponse
{
    public DateTime RangeStart { get; set; }
    public DateTime RangeEnd { get; set; }
    public int TotalScheduled { get; set; }
    public List<CapacitySlot> WorkOrders { get; set; } = new();
}

public class CalendarService
{
    private readonly AgriDbContext _db;

    public CalendarService(AgriDbContext db)
    {
        _db = db;
    }

    public async Task<CenterCapacityResponse> GetCenterCapacityAsync(DateTime start, DateTime end)
    {
        var workOrders = await _db.WorkOrders
            .AsNoTracking()
            .Where(w =>
                w.ScheduledStartDate < end &&
                w.ScheduledEndDate > start &&
                w.Status != Core.Enums.WorkStatus.Cancelled)
            .Select(w => new CapacitySlot
            {
                WorkOrderId = w.Id,
                CenterId = w.CenterId,
                EquipmentId = w.EquipmentId,
                ResponsibleUserId = w.ResponsibleUserId,
                Description = w.Description,
                Type = w.Type.ToString(),
                Status = w.Status.ToString(),
                ScheduledStartDate = w.ScheduledStartDate,
                ScheduledEndDate = w.ScheduledEndDate
            })
            .ToListAsync();

        return new CenterCapacityResponse
        {
            RangeStart = start,
            RangeEnd = end,
            TotalScheduled = workOrders.Count,
            WorkOrders = workOrders
        };
    }
}
