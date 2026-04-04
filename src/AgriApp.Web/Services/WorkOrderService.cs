using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly HttpClient _http;
    private readonly ILogger<WorkOrderService> _logger;

    public WorkOrderService(HttpClient http, ILogger<WorkOrderService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<WorkOrderListItem>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<WorkOrderListItem>>("api/work-orders")
                   ?? new List<WorkOrderListItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load work orders");
            throw new ApplicationException("Could not load work orders. Please try again.", ex);
        }
    }

    public async Task<WorkOrderDetail?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<WorkOrderDetail>($"api/work-orders/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load work order {Id}", id);
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
            _logger.LogWarning("Create work order failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating work order");
            return (false, "An unexpected error occurred while creating the work order.", null);
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
            _logger.LogWarning("Update work order status failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating work order {Id} status", id);
            return (false, "An unexpected error occurred while updating the work order.", null);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/work-orders/{id}");
            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Delete work order {Id} failed: {Error}", id, error);
            return (false, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting work order {Id}", id);
            return (false, "An unexpected error occurred while deleting the work order.");
        }
    }
}
