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

    public async Task<Equipment> CreateAsync(string name, EquipmentCategory category, decimal hourlyRate, int centerId)
    {
        var equipment = new Equipment
        {
            Name = name,
            Category = category,
            HourlyRate = hourlyRate,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        };
        return await _repo.CreateAsync(equipment);
    }

    public async Task<Equipment?> UpdateAsync(int id, string? name, EquipmentCategory? category, decimal? hourlyRate)
    {
        return await _repo.UpdateAsync(id, equipment =>
        {
            if (name != null) equipment.Name = name;
            if (category.HasValue) equipment.Category = category.Value;
            if (hourlyRate.HasValue) equipment.HourlyRate = hourlyRate.Value;
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
