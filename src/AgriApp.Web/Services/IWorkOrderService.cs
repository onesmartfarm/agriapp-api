namespace AgriApp.Web.Services;

public interface IWorkOrderService
{
    Task<List<WorkOrderListItem>> GetAllAsync();
    Task<WorkOrderDetail?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, WorkOrderDetail? WorkOrder)> CreateAsync(WorkOrderFormRequest request);
    Task<(bool Success, string? Error, WorkOrderDetail? WorkOrder)> UpdateStatusAsync(int id, string status);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
