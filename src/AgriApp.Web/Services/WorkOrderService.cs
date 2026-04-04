using System.Net.Http.Json;

namespace AgriApp.Web.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly HttpClient _http;

    public WorkOrderService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<WorkOrderListItem>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<WorkOrderListItem>>("api/work-orders")
                   ?? new List<WorkOrderListItem>();
        }
        catch
        {
            return new List<WorkOrderListItem>();
        }
    }

    public async Task<WorkOrderListItem?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<WorkOrderListItem>($"api/work-orders/{id}");
        }
        catch
        {
            return null;
        }
    }
}
