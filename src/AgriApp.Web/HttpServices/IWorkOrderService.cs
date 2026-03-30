using AgriApp.Web.Models.WorkOrders;

namespace AgriApp.Web.HttpServices;

public interface IWorkOrderService
{
    Task<List<WorkOrderDto>?> GetAllAsync();
    Task<WorkOrderDto?> GetByIdAsync(int id);
    Task<WorkOrderDto?> CreateAsync(CreateWorkOrderRequest request);
    Task<WorkOrderDto?> UpdateStatusAsync(int id, string status);
}
