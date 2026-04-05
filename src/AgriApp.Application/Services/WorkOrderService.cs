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
    {
        var rows = await _db.WorkOrders
            .AsNoTracking()
            .Include(w => w.Center)
            .Include(w => w.Implement)
            .Include(w => w.Tractor)
            .Include(w => w.ServiceActivity)
            .OrderByDescending(w => w.Id)
            .ToListAsync();
        return rows.Select(MapToResponse).ToList();
    }

    public async Task<WorkOrderResponse?> GetByIdAsync(int id)
    {
        var w = await _db.WorkOrders
            .AsNoTracking()
            .Include(x => x.Center)
            .Include(x => x.Implement)
            .Include(x => x.Tractor)
            .Include(x => x.ServiceActivity)
            .FirstOrDefaultAsync(x => x.Id == id);
        return w == null ? null : MapToResponse(w);
    }

    public async Task<List<WorkOrderResponse>> GetByCustomerIdAsync(int customerId)
    {
        var rows = await _db.WorkOrders
            .AsNoTracking()
            .Include(w => w.Center)
            .Include(w => w.Implement)
            .Include(w => w.Tractor)
            .Include(w => w.ServiceActivity)
            .Where(w => w.CustomerId == customerId)
            .OrderByDescending(w => w.Id)
            .ToListAsync();
        return rows.Select(MapToResponse).ToList();
    }

    public async Task<WorkOrderResponse> CreateWorkOrderAsync(CreateWorkOrderRequest request, int centerId)
    {
        if (!Enum.TryParse<WorkOrderType>(request.Type, out var type))
            throw new ArgumentException($"Invalid WorkOrderType: {request.Type}");

        if (request.ScheduledEndDate <= request.ScheduledStartDate)
            throw new ArgumentException("ScheduledEndDate must be after ScheduledStartDate.");

        await ValidateServiceActivityForCenterAsync(request.ServiceActivityId, centerId);
        await ValidateImplementAsync(request.ImplementId, centerId);
        await ValidateTractorAsync(request.TractorId, centerId);

        await EnforceDoubleBookingFirewall(
            request.ImplementId,
            request.TractorId,
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
            ServiceActivityId = request.ServiceActivityId,
            ImplementId = request.ImplementId,
            TractorId = request.TractorId,
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

        return await GetByIdAsync(workOrder.Id)
            ?? throw new InvalidOperationException("Failed to load work order after create.");
    }

    public async Task<WorkOrderResponse?> UpdateWorkOrderAsync(int id, UpdateWorkOrderRequest request)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (workOrder == null) return null;

        await ValidateCustomerForCenterAsync(request.CustomerId, workOrder.CenterId);

        workOrder.CustomerId = request.CustomerId;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (workOrder == null) return false;
        _db.WorkOrders.Remove(workOrder);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkOrderResponse?> UpdateStatusAsync(int id, WorkStatus status)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (workOrder == null) return null;

        if (status == WorkStatus.InProgress && workOrder.ActualStartDate == null)
            workOrder.ActualStartDate = DateTime.UtcNow;

        if (status == WorkStatus.Completed && workOrder.ActualEndDate == null)
            workOrder.ActualEndDate = DateTime.UtcNow;

        workOrder.Status = status;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private async Task ValidateCustomerForCenterAsync(int? customerId, int centerId)
    {
        if (!customerId.HasValue) return;

        var ok = await _db.Customers.AsNoTracking()
            .AnyAsync(c => c.Id == customerId.Value && c.CenterId == centerId);
        if (!ok)
            throw new ArgumentException("Customer not found or does not belong to this center.");
    }

    private async Task ValidateServiceActivityForCenterAsync(int? serviceActivityId, int centerId)
    {
        if (!serviceActivityId.HasValue) return;

        var ok = await _db.ServiceActivities.AsNoTracking()
            .AnyAsync(a => a.Id == serviceActivityId.Value && a.CenterId == centerId);
        if (!ok)
            throw new ArgumentException("Service activity not found or does not belong to this center.");
    }

    private async Task ValidateImplementAsync(int? implementId, int centerId)
    {
        if (!implementId.HasValue) return;

        var eq = await _db.Equipment.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == implementId.Value && e.CenterId == centerId);
        if (eq == null)
            throw new ArgumentException("Implement (equipment) not found or does not belong to this center.");
        if (!eq.IsImplement)
            throw new ArgumentException("Selected implement must be marked as an implement (attachment/tool).");
    }

    private async Task ValidateTractorAsync(int? tractorId, int centerId)
    {
        if (!tractorId.HasValue) return;

        var eq = await _db.Equipment.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == tractorId.Value && e.CenterId == centerId);
        if (eq == null)
            throw new ArgumentException("Tractor (equipment) not found or does not belong to this center.");
        if (eq.IsImplement)
            throw new ArgumentException("Selected tractor must not be marked as an implement.");
    }

    private async Task EnforceDoubleBookingFirewall(
        int? implementId,
        int? tractorId,
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

        var conflicts = await query
            .Where(w =>
                w.ResponsibleUserId == responsibleUserId ||
                (implementId.HasValue && w.ImplementId == implementId) ||
                (tractorId.HasValue && w.TractorId == tractorId))
            .AnyAsync();

        if (conflicts)
            throw new InvalidOperationException("Schedule conflict detected.");
    }

    private static WorkOrderResponse MapToResponse(WorkOrder w)
    {
        var sym = string.IsNullOrWhiteSpace(w.Center?.CurrencySymbol) ? "₹" : w.Center.CurrencySymbol;
        return new WorkOrderResponse
        {
            Id = w.Id,
            CenterId = w.CenterId,
            CustomerId = w.CustomerId,
            InquiryId = w.InquiryId,
            ServiceActivityId = w.ServiceActivityId,
            ImplementId = w.ImplementId,
            TractorId = w.TractorId,
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
            UpdatedAt = w.UpdatedAt,
            ServiceActivityName = w.ServiceActivity?.Name,
            ImplementName = w.Implement?.Name,
            TractorName = w.Tractor?.Name,
            CurrencySymbol = sym
        };
    }
}
