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

    public async Task<WorkOrderDetail?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<WorkOrderDetail>($"api/work-orders/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error, WorkOrderDetail? WorkOrder)> CreateAsync(WorkOrderFormRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/work-orders", new
            {
                request.ResponsibleUserId,
                request.EquipmentId,
                request.InquiryId,
                request.CenterId,
                request.Description,
                request.Type,
                ScheduledStartDate = request.ScheduledStartDate.ToUniversalTime(),
                ScheduledEndDate = request.ScheduledEndDate.ToUniversalTime(),
                request.TotalMaterialCost
            });

            if (response.IsSuccessStatusCode)
            {
                var wo = await response.Content.ReadFromJsonAsync<WorkOrderDetail>();
                return (true, null, wo);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error, WorkOrderDetail? WorkOrder)> UpdateStatusAsync(int id, string status)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/work-orders/{id}/status", new { Status = status });

            if (response.IsSuccessStatusCode)
            {
                var wo = await response.Content.ReadFromJsonAsync<WorkOrderDetail>();
                return (true, null, wo);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }
}
