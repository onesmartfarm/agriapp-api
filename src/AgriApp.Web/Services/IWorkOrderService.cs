namespace AgriApp.Web.Services;

public interface IWorkOrderService
{
    Task<List<WorkOrderListItem>> GetAllAsync();
    Task<WorkOrderListItem?> GetByIdAsync(int id);
}
