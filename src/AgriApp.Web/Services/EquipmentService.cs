using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AgriApp.Web.Services;

public class EquipmentService : IEquipmentService
{
    private readonly HttpClient _http;
    private readonly ILogger<EquipmentService> _logger;

    public EquipmentService(HttpClient http, ILogger<EquipmentService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<EquipmentResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<EquipmentResponse>>("api/equipment")
                   ?? new List<EquipmentResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load equipment list");
            throw new ApplicationException("Could not load equipment. Please try again.", ex);
        }
    }

    public async Task<EquipmentResponse?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<EquipmentResponse>($"api/equipment/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load equipment {Id}", id);
            return null;
        }
    }

    public async Task<(bool Success, string? Error, EquipmentResponse? Equipment)> CreateAsync(CreateEquipmentRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/equipment", new
            {
                request.Name,
                request.Category,
                request.HourlyRate,
                request.CenterId,
                request.VendorId,
                request.PurchaseCost,
                request.PurchaseDate
            });

            if (response.IsSuccessStatusCode)
            {
                var equipment = await response.Content.ReadFromJsonAsync<EquipmentResponse>();
                return (true, null, equipment);
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Create equipment failed: {Error}", error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating equipment");
            return (false, "An unexpected error occurred while creating equipment.", null);
        }
    }

    public async Task<(bool Success, string? Error, EquipmentResponse? Equipment)> UpdateAsync(int id, UpdateEquipmentRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/equipment/{id}", new
            {
                request.Name,
                request.Category,
                request.HourlyRate,
                request.VendorId,
                request.PurchaseCost,
                request.PurchaseDate
            });

            if (response.IsSuccessStatusCode)
            {
                var equipment = await response.Content.ReadFromJsonAsync<EquipmentResponse>();
                return (true, null, equipment);
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Update equipment {Id} failed: {Error}", id, error);
            return (false, error, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating equipment {Id}", id);
            return (false, "An unexpected error occurred while updating equipment.", null);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/equipment/{id}");
            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Delete equipment {Id} failed: {Error}", id, error);
            return (false, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting equipment {Id}", id);
            return (false, "An unexpected error occurred while deleting equipment.");
        }
    }

    public async Task<(bool Success, string? Error, EquipmentQuoteResult? Quote)> GetQuoteAsync(int id, decimal hours)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/equipment/{id}/quote", new { Hours = hours });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Quote for equipment {Id} failed: {Error}", id, error);
                return (false, error, null);
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;

            var pricing = root.GetProperty("pricing");
            var commission = root.GetProperty("commission");

            var quote = new EquipmentQuoteResult(
                Hours: root.GetProperty("hours").GetDecimal(),
                Pricing: new QuoteGstBreakdown(
                    BaseAmount: pricing.GetProperty("baseAmount").GetDecimal(),
                    Cgst: pricing.GetProperty("cgst").GetDecimal(),
                    Sgst: pricing.GetProperty("sgst").GetDecimal(),
                    TotalGst: pricing.GetProperty("totalGst").GetDecimal(),
                    GrandTotal: pricing.GetProperty("grandTotal").GetDecimal()
                ),
                Commission: new QuoteCommissionResult(
                    RentalAmount: commission.GetProperty("rentalAmount").GetDecimal(),
                    CommissionRate: commission.GetProperty("commissionRate").GetDecimal(),
                    CommissionAmount: commission.GetProperty("commissionAmount").GetDecimal(),
                    NetToCompany: commission.GetProperty("netToCompany").GetDecimal()
                )
            );

            return (true, null, quote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting quote for equipment {Id}", id);
            return (false, "An unexpected error occurred while calculating the quote.", null);
        }
    }
}
