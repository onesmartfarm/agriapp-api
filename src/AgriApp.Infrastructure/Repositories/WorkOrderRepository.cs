using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Infrastructure.Repositories;

public class WorkOrderRepository
{
    private readonly AgriDbContext _context;

    public WorkOrderRepository(AgriDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkOrder>> GetAllAsync()
        => await _context.WorkOrders.AsNoTracking().ToListAsync();

    public async Task<WorkOrder?> GetByIdAsync(int id)
        => await _context.WorkOrders.FindAsync(id);

    public async Task<WorkOrder> CreateAsync(WorkOrder workOrder)
    {
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();
        return workOrder;
    }

    public async Task<WorkOrder?> UpdateStatusAsync(int id, WorkStatus status)
    {
        var workOrder = await _context.WorkOrders.FindAsync(id);
        if (workOrder == null) return null;
        workOrder.Status = status;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return workOrder;
    }
}
