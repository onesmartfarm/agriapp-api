using System.Net.Http.Json;
using System.Text.Json;
using AgriApp.Web.Models.WorkOrders;

namespace AgriApp.Web.HttpServices;

public class WorkOrderService : IWorkOrderService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public WorkOrderService(HttpClient http) => _http = http;

    public async Task<List<WorkOrderDto>?> GetAllAsync()
    {
        var response = await _http.GetAsync("api/work-orders");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<WorkOrderDto>>(JsonOpts);
    }

    public async Task<WorkOrderDto?> GetByIdAsync(int id)
    {
        var response = await _http.GetAsync($"api/work-orders/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOpts);
    }

    public async Task<WorkOrderDto?> CreateAsync(CreateWorkOrderRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/work-orders", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOpts);
    }

    public async Task<WorkOrderDto?> UpdateStatusAsync(int id, string status)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/work-orders/{id}/status", new { Status = status });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkOrderDto>(JsonOpts);
    }
}
