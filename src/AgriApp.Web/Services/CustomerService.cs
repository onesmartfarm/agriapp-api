using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class CustomerService : ICustomerService
{
    private readonly HttpClient _http;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(HttpClient http, ILogger<CustomerService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<CustomerResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CustomerResponse>>("api/customers")
                   ?? new List<CustomerResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load customers");
            throw new ApplicationException("Could not load customers. Please try again.", ex);
        }
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<CustomerResponse>($"api/customers/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load customer {Id}", id);
            return null;
        }
    }

    public async Task<CustomerFinancialSummaryResponse?> GetSummaryAsync(int customerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<CustomerFinancialSummaryResponse>($"api/customers/{customerId}/summary");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load customer {Id} summary", customerId);
            return null;
        }
    }

    public async Task<List<InquiryResponse>> GetInquiriesForCustomerAsync(int customerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<InquiryResponse>>($"api/customers/{customerId}/inquiries")
                   ?? new List<InquiryResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inquiries for customer {Id}", customerId);
            throw new ApplicationException("Could not load customer inquiries.", ex);
        }
    }

    public async Task<List<WorkOrderListItem>> GetWorkOrdersForCustomerAsync(int customerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<WorkOrderListItem>>($"api/customers/{customerId}/workorders")
                   ?? new List<WorkOrderListItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load work orders for customer {Id}", customerId);
            throw new ApplicationException("Could not load customer work orders.", ex);
        }
    }

    public async Task<List<CustomerLedgerEntryResponse>> GetLedgerAsync(int customerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CustomerLedgerEntryResponse>>($"api/customers/{customerId}/ledger")
                   ?? new List<CustomerLedgerEntryResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ledger for customer {Id}", customerId);
            throw new ApplicationException("Could not load customer ledger.", ex);
        }
    }

    public async Task<(bool Success, string? Error, CustomerResponse? Data)> CreateAsync(CreateCustomerRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/customers", request);
            if (response.IsSuccessStatusCode)
            {
                var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
                return (true, null, customer);
            }
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Create customer failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating customer");
            return (false, "An unexpected error occurred while creating the customer.", null);
        }
    }

    public async Task<(bool Success, string? Error, CustomerResponse? Data)> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/customers/{id}", request);
            if (response.IsSuccessStatusCode)
            {
                var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
                return (true, null, customer);
            }
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Update customer {Id} failed: {Error}", id, error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating customer {Id}", id);
            return (false, "An unexpected error occurred while updating the customer.", null);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/customers/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting customer {Id}", id);
            return false;
        }
    }
}
