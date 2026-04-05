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
            return await _http.GetFromJsonAsync<List<WorkOrderListItem>>("api/workorders")
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
            return await _http.GetFromJsonAsync<WorkOrderDetail>($"api/workorders/{id}");
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
            var response = await _http.PostAsJsonAsync("api/workorders", new
            {
                request.ResponsibleUserId,
                request.ServiceActivityId,
                request.ImplementId,
                request.TractorId,
                request.InquiryId,
                request.CustomerId,
                request.CenterId,
                request.Description,
                request.Type,
                ScheduledStartDate = request.ScheduledStartDate.ToUniversalTime(),
                ScheduledEndDate = request.ScheduledEndDate.ToUniversalTime(),
                request.TotalMaterialCost,
                TimeLogs = request.TimeLogs?.Select(t => new
                {
                    StartTime = t.StartTime.ToUniversalTime(),
                    EndTime = t.EndTime.ToUniversalTime(),
                    t.LogType,
                    t.Notes
                }).ToList()
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

    public async Task<(bool Success, string? Error, WorkOrderDetail? WorkOrder)> UpdateAsync(int id, WorkOrderPatchRequest request)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/workorders/{id}", new { request.CustomerId });

            if (response.IsSuccessStatusCode)
            {
                var wo = await response.Content.ReadFromJsonAsync<WorkOrderDetail>();
                return (true, null, wo);
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Update work order {Id} failed: {Error}", id, error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating work order {Id}", id);
            return (false, "An unexpected error occurred while updating the work order.", null);
        }
    }

    public async Task<(bool Success, string? Error, WorkOrderDetail? WorkOrder)> UpdateStatusAsync(int id, string status)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/workorders/{id}/status", new { Status = status });

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
            var response = await _http.DeleteAsync($"api/workorders/{id}");
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
