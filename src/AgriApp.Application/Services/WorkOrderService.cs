using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class WorkOrderService
{
    private readonly AgriDbContext _db;

    public WorkOrderService(AgriDbContext db)
    {
        _db = db;
    }

    public async Task<List<WorkOrderResponse>> GetAllAsync()
        => await _db.WorkOrders
            .AsNoTracking()
            .Select(w => MapToResponse(w))
            .ToListAsync();

    public async Task<WorkOrderResponse?> GetByIdAsync(int id)
    {
        var w = await _db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return w == null ? null : MapToResponse(w);
    }

    public async Task<WorkOrderResponse> CreateWorkOrderAsync(CreateWorkOrderRequest request, int centerId)
    {
        if (!Enum.TryParse<WorkOrderType>(request.Type, out var type))
            throw new ArgumentException($"Invalid WorkOrderType: {request.Type}");

        if (request.ScheduledEndDate <= request.ScheduledStartDate)
            throw new ArgumentException("ScheduledEndDate must be after ScheduledStartDate.");

        await EnforceDoubleBookingFirewall(
            request.EquipmentId,
            request.ResponsibleUserId,
            request.ScheduledStartDate,
            request.ScheduledEndDate,
            excludeId: null);

        await ValidateCustomerForCenterAsync(request.CustomerId, centerId);

        var workOrder = new WorkOrder
        {
            CenterId = centerId,
            CustomerId = request.CustomerId,
            InquiryId = request.InquiryId,
            EquipmentId = request.EquipmentId,
            ResponsibleUserId = request.ResponsibleUserId,
            Description = request.Description,
            Type = type,
            Status = WorkStatus.Scheduled,
            ScheduledStartDate = request.ScheduledStartDate,
            ScheduledEndDate = request.ScheduledEndDate,
            TotalMaterialCost = request.TotalMaterialCost,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkOrders.Add(workOrder);
        await _db.SaveChangesAsync();

        if (request.TimeLogs is { Count: > 0 } logs)
        {
            foreach (var tl in logs)
            {
                if (!Enum.TryParse<WorkTimeLogType>(tl.LogType, ignoreCase: true, out var logType))
                    throw new ArgumentException($"Invalid time log type: {tl.LogType}");

                if (tl.EndTime <= tl.StartTime)
                    throw new ArgumentException("Each time log must have EndTime after StartTime.");

                _db.WorkOrderTimeLogs.Add(new WorkOrderTimeLog
                {
                    WorkOrderId = workOrder.Id,
                    StartTime = tl.StartTime,
                    EndTime = tl.EndTime,
                    LogType = logType,
                    Notes = tl.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        return MapToResponse(workOrder);
    }

    public async Task<WorkOrderResponse?> UpdateWorkOrderAsync(int id, UpdateWorkOrderRequest request)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (workOrder == null) return null;

        await ValidateCustomerForCenterAsync(request.CustomerId, workOrder.CenterId);

        workOrder.CustomerId = request.CustomerId;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToResponse(workOrder);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Use FirstOrDefaultAsync so Global Query Filter (CenterId isolation) is applied
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (workOrder == null) return false;
        _db.WorkOrders.Remove(workOrder);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkOrderResponse?> UpdateStatusAsync(int id, WorkStatus status)
    {
        // Use FirstOrDefaultAsync so Global Query Filter (CenterId isolation) is applied
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (workOrder == null) return null;

        if (status == WorkStatus.InProgress && workOrder.ActualStartDate == null)
            workOrder.ActualStartDate = DateTime.UtcNow;

        if (status == WorkStatus.Completed && workOrder.ActualEndDate == null)
            workOrder.ActualEndDate = DateTime.UtcNow;

        workOrder.Status = status;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToResponse(workOrder);
    }

    private async Task ValidateCustomerForCenterAsync(int? customerId, int centerId)
    {
        if (!customerId.HasValue) return;

        var ok = await _db.Customers.AsNoTracking()
            .AnyAsync(c => c.Id == customerId.Value && c.CenterId == centerId);
        if (!ok)
            throw new ArgumentException("Customer not found or does not belong to this center.");
    }

    private async Task EnforceDoubleBookingFirewall(
        int? equipmentId,
        int responsibleUserId,
        DateTime start,
        DateTime end,
        int? excludeId)
    {
        var query = _db.WorkOrders
            .AsNoTracking()
            .Where(w =>
                w.Status != WorkStatus.Cancelled &&
                w.ScheduledStartDate < end &&
                w.ScheduledEndDate > start);

        if (excludeId.HasValue)
            query = query.Where(w => w.Id != excludeId.Value);

        bool conflict;

        if (equipmentId.HasValue)
        {
            conflict = await query.AnyAsync(w =>
                w.EquipmentId == equipmentId ||
                w.ResponsibleUserId == responsibleUserId);
        }
        else
        {
            conflict = await query.AnyAsync(w =>
                w.ResponsibleUserId == responsibleUserId);
        }

        if (conflict)
            throw new InvalidOperationException("Schedule conflict detected.");
    }

    private static WorkOrderResponse MapToResponse(WorkOrder w) => new()
    {
        Id = w.Id,
        CenterId = w.CenterId,
        CustomerId = w.CustomerId,
        InquiryId = w.InquiryId,
        EquipmentId = w.EquipmentId,
        ResponsibleUserId = w.ResponsibleUserId,
        Description = w.Description,
        Type = w.Type.ToString(),
        Status = w.Status.ToString(),
        ScheduledStartDate = w.ScheduledStartDate,
        ScheduledEndDate = w.ScheduledEndDate,
        ActualStartDate = w.ActualStartDate,
        ActualEndDate = w.ActualEndDate,
        TotalMaterialCost = w.TotalMaterialCost,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt
    };
}
