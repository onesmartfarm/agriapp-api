using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Repositories;

namespace AgriApp.Application.Services;

public class EquipmentService
{
    private readonly EquipmentRepository _repo;

    public EquipmentService(EquipmentRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<EquipmentApiResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(ToResponse).ToList();
    }

    public async Task<EquipmentApiResponse?> GetByIdAsync(int id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ToResponse(e);
    }

    public async Task<EquipmentApiResponse> CreateAsync(
        string name,
        EquipmentCategory category,
        decimal hourlyRate,
        int centerId,
        int? vendorId = null,
        decimal? purchaseCost = null,
        DateTime? purchaseDate = null)
    {
        var equipment = new Equipment
        {
            Name = name,
            Category = category,
            HourlyRate = hourlyRate,
            CenterId = centerId,
            VendorId = vendorId,
            PurchaseCost = purchaseCost,
            PurchaseDate = purchaseDate,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.CreateAsync(equipment);
        var reloaded = await _repo.GetByIdAsync(created.Id)
            ?? throw new InvalidOperationException("Failed to reload equipment after create.");
        return ToResponse(reloaded);
    }

    public async Task<EquipmentApiResponse?> UpdateAsync(
        int id,
        string? name,
        EquipmentCategory? category,
        decimal? hourlyRate,
        int? vendorId = null,
        decimal? purchaseCost = null,
        DateTime? purchaseDate = null)
    {
        var updated = await _repo.UpdateAsync(id, equipment =>
        {
            if (name != null) equipment.Name = name;
            if (category.HasValue) equipment.Category = category.Value;
            if (hourlyRate.HasValue) equipment.HourlyRate = hourlyRate.Value;
            if (vendorId.HasValue) equipment.VendorId = vendorId.Value;
            if (purchaseCost.HasValue) equipment.PurchaseCost = purchaseCost.Value;
            if (purchaseDate.HasValue) equipment.PurchaseDate = purchaseDate.Value;
        });
        if (updated == null) return null;
        var reloaded = await _repo.GetByIdAsync(updated.Id);
        return reloaded == null ? null : ToResponse(reloaded);
    }

    public async Task<bool> DeleteAsync(int id)
        => await _repo.DeleteAsync(id);

    public (GstBreakdown Gst, CommissionResult Commission) GetQuote(decimal hourlyRate, decimal hours)
    {
        var gst = GstCalculator.CalculateRental(hourlyRate, hours);
        var commission = CommissionCalculator.Calculate(gst.BaseAmount);
        return (gst, commission);
    }

    private static EquipmentApiResponse ToResponse(Equipment e)
    {
        var sym = string.IsNullOrWhiteSpace(e.Center?.CurrencySymbol) ? "₹" : e.Center.CurrencySymbol;
        return new EquipmentApiResponse
        {
            Id = e.Id,
            Name = e.Name,
            Category = e.Category.ToString(),
            HourlyRate = e.HourlyRate,
            CenterId = e.CenterId,
            CenterName = e.Center?.Name ?? string.Empty,
            CurrencySymbol = sym,
            VendorId = e.VendorId,
            PurchaseCost = e.PurchaseCost,
            PurchaseDate = e.PurchaseDate,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
