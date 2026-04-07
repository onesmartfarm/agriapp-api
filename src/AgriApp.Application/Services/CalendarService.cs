using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class CapacitySlot
{
    public int WorkOrderId { get; set; }
    public int CenterId { get; set; }
    public int? ImplementId { get; set; }
    public int? TractorId { get; set; }
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

/// <summary>One swimlane row: equipment + scheduled blocks for the query window.</summary>
public class EquipmentTimelineResponse
{
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public bool IsImplement { get; set; }
    public List<TimelineBlock> Blocks { get; set; } = new();
}

/// <summary>
/// A segment on the timeline: scheduled work, travel buffer, or equipment breakdown from <see cref="WorkOrderTimeLog"/>.
/// </summary>
public class TimelineBlock
{
    /// <summary>Work, Travel, or Breakdown.</summary>
    public string BlockType { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? WorkOrderId { get; set; }
    public string? CustomerName { get; set; }
    public string? ServiceActivityName { get; set; }

    /// <summary>Shared when both tractor and implement are assigned (pair highlight in UI).</summary>
    public string? PairingKey { get; set; }
}

public class EquipmentTimelineEnvelope
{
    public DateTime RangeStart { get; set; }
    public DateTime RangeEnd { get; set; }
    public List<EquipmentTimelineResponse> Equipment { get; set; } = new();
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
                ImplementId = w.ImplementId,
                TractorId = w.TractorId,
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

    private static readonly TimeSpan TravelBuffer = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Equipment dispatch swimlanes: work orders and breakdown intervals from work-order time logs.
    /// Travel buffers are 30 minutes before/after each scheduled work interval (clipped to the query range).
    /// </summary>
    public async Task<EquipmentTimelineEnvelope> GetEquipmentTimelineAsync(DateTime rangeStart, DateTime rangeEnd)
    {
        if (rangeEnd <= rangeStart)
            throw new ArgumentException("End must be after start.", nameof(rangeEnd));

        if ((rangeEnd - rangeStart).TotalDays > 7)
            throw new ArgumentException("Timeline range cannot exceed 7 days.", nameof(rangeEnd));

        var equipment = await _db.Equipment
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name, e.IsImplement })
            .ToListAsync();

        var workOrders = await _db.WorkOrders
            .AsNoTracking()
            .Where(w =>
                w.Status != WorkStatus.Cancelled &&
                w.ScheduledStartDate < rangeEnd &&
                w.ScheduledEndDate > rangeStart &&
                (w.TractorId != null || w.ImplementId != null))
            .Select(w => new
            {
                w.Id,
                w.TractorId,
                w.ImplementId,
                w.ScheduledStartDate,
                w.ScheduledEndDate,
                CustomerName = w.Customer != null ? w.Customer.Name : (string?)null,
                ActivityName = w.ServiceActivity != null ? w.ServiceActivity.Name : (string?)null
            })
            .ToListAsync();

        var breakdowns = await _db.WorkOrderTimeLogs
            .AsNoTracking()
            .Where(t =>
                t.LogType == WorkTimeLogType.Breakdown &&
                t.StartTime < rangeEnd &&
                t.EndTime > rangeStart &&
                t.WorkOrder.Status != WorkStatus.Cancelled)
            .Select(t => new
            {
                t.WorkOrderId,
                t.StartTime,
                t.EndTime,
                TractorId = t.WorkOrder.TractorId,
                ImplementId = t.WorkOrder.ImplementId,
                CustomerName = t.WorkOrder.Customer != null ? t.WorkOrder.Customer.Name : (string?)null,
                ActivityName = t.WorkOrder.ServiceActivity != null ? t.WorkOrder.ServiceActivity.Name : (string?)null
            })
            .ToListAsync();

        var byEquipment = equipment.ToDictionary(
            e => e.Id,
            e => new EquipmentTimelineResponse
            {
                EquipmentId = e.Id,
                EquipmentName = e.Name,
                IsImplement = e.IsImplement,
                Blocks = new List<TimelineBlock>()
            });

        static string? PairingKey(int workOrderId, int? tractorId, int? implementId) =>
            tractorId.HasValue && implementId.HasValue ? $"wo-pair-{workOrderId}" : null;

        static void TryAddBlock(
            List<TimelineBlock> blocks,
            string blockType,
            DateTime segStart,
            DateTime segEnd,
            DateTime windowStart,
            DateTime windowEnd,
            int? workOrderId,
            string? customerName,
            string? activityName,
            string? pairingKey)
        {
            var clipStart = segStart < windowStart ? windowStart : segStart;
            var clipEnd = segEnd > windowEnd ? windowEnd : segEnd;
            if (clipEnd <= clipStart)
                return;

            blocks.Add(new TimelineBlock
            {
                BlockType = blockType,
                StartTime = clipStart,
                EndTime = clipEnd,
                WorkOrderId = workOrderId,
                CustomerName = customerName,
                ServiceActivityName = activityName,
                PairingKey = pairingKey
            });
        }

        foreach (var wo in workOrders)
        {
            var pairKey = PairingKey(wo.Id, wo.TractorId, wo.ImplementId);
            var cust = wo.CustomerName;
            var act = wo.ActivityName;

            void AddForEquipment(int equipmentId)
            {
                if (!byEquipment.TryGetValue(equipmentId, out var row))
                    return;

                var travelBeforeStart = wo.ScheduledStartDate - TravelBuffer;
                var travelBeforeEnd = wo.ScheduledStartDate;
                TryAddBlock(row.Blocks, "Travel", travelBeforeStart, travelBeforeEnd, rangeStart, rangeEnd,
                    wo.Id, cust, act, pairKey);

                TryAddBlock(row.Blocks, "Work", wo.ScheduledStartDate, wo.ScheduledEndDate, rangeStart, rangeEnd,
                    wo.Id, cust, act, pairKey);

                var travelAfterStart = wo.ScheduledEndDate;
                var travelAfterEnd = wo.ScheduledEndDate + TravelBuffer;
                TryAddBlock(row.Blocks, "Travel", travelAfterStart, travelAfterEnd, rangeStart, rangeEnd,
                    wo.Id, cust, act, pairKey);
            }

            if (wo.TractorId is int tid)
                AddForEquipment(tid);
            if (wo.ImplementId is int iid)
                AddForEquipment(iid);
        }

        foreach (var bd in breakdowns)
        {
            var pairKey = PairingKey(bd.WorkOrderId, bd.TractorId, bd.ImplementId);
            void AddBreakdownForEquipment(int equipmentId)
            {
                if (!byEquipment.TryGetValue(equipmentId, out var row))
                    return;
                TryAddBlock(row.Blocks, "Breakdown", bd.StartTime, bd.EndTime, rangeStart, rangeEnd,
                    bd.WorkOrderId, bd.CustomerName, bd.ActivityName, pairKey);
            }

            if (bd.TractorId is int tid)
                AddBreakdownForEquipment(tid);
            if (bd.ImplementId is int iid)
                AddBreakdownForEquipment(iid);
        }

        foreach (var row in byEquipment.Values)
        {
            row.Blocks.Sort((a, b) =>
            {
                var c = a.StartTime.CompareTo(b.StartTime);
                return c != 0 ? c : string.CompareOrdinal(a.BlockType, b.BlockType);
            });
        }

        return new EquipmentTimelineEnvelope
        {
            RangeStart = rangeStart,
            RangeEnd = rangeEnd,
            Equipment = byEquipment.Values.OrderBy(r => r.EquipmentName).ToList()
        };
    }
}
