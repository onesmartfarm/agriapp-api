using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Repositories;

namespace AgriApp.Application.Services;

public class WorkOrderService
{
    private readonly WorkOrderRepository _repo;

    public WorkOrderService(WorkOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<WorkOrder>> GetAllAsync()
        => await _repo.GetAllAsync();

    public async Task<WorkOrder?> GetByIdAsync(int id)
        => await _repo.GetByIdAsync(id);

    public async Task<WorkOrder> CreateAsync(int equipmentId, int staffId, string description, int centerId)
    {
        var workOrder = new WorkOrder
        {
            EquipmentId = equipmentId,
            StaffId = staffId,
            Description = description,
            CenterId = centerId,
            Status = WorkStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        return await _repo.CreateAsync(workOrder);
    }

    public async Task<WorkOrder?> UpdateStatusAsync(int id, WorkStatus status)
        => await _repo.UpdateStatusAsync(id, status);
}
