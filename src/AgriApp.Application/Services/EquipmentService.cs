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

    public async Task<List<Equipment>> GetAllAsync()
        => await _repo.GetAllAsync();

    public async Task<Equipment?> GetByIdAsync(int id)
        => await _repo.GetByIdAsync(id);

    public async Task<Equipment> CreateAsync(
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
        return await _repo.CreateAsync(equipment);
    }

    public async Task<Equipment?> UpdateAsync(
        int id,
        string? name,
        EquipmentCategory? category,
        decimal? hourlyRate,
        int? vendorId = null,
        decimal? purchaseCost = null,
        DateTime? purchaseDate = null)
    {
        return await _repo.UpdateAsync(id, equipment =>
        {
            if (name != null) equipment.Name = name;
            if (category.HasValue) equipment.Category = category.Value;
            if (hourlyRate.HasValue) equipment.HourlyRate = hourlyRate.Value;
            if (vendorId.HasValue) equipment.VendorId = vendorId.Value;
            if (purchaseCost.HasValue) equipment.PurchaseCost = purchaseCost.Value;
            if (purchaseDate.HasValue) equipment.PurchaseDate = purchaseDate.Value;
        });
    }

    public async Task<bool> DeleteAsync(int id)
        => await _repo.DeleteAsync(id);

    public (GstBreakdown Gst, CommissionResult Commission) GetQuote(decimal hourlyRate, decimal hours)
    {
        var gst = GstCalculator.CalculateRental(hourlyRate, hours);
        var commission = CommissionCalculator.Calculate(gst.BaseAmount);
        return (gst, commission);
    }
}
