using AgriApp.Core.Entities;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Infrastructure.Repositories;

public class EquipmentRepository
{
    private readonly AgriDbContext _context;

    public EquipmentRepository(AgriDbContext context)
    {
        _context = context;
    }

    public async Task<List<Equipment>> GetAllAsync()
        => await _context.Equipment
            .AsNoTracking()
            .Include(e => e.Center)
            .ToListAsync();

    public async Task<Equipment?> GetByIdAsync(int id)
        => await _context.Equipment
            .Include(e => e.Center)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Equipment> CreateAsync(Equipment equipment)
    {
        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();
        return equipment;
    }

    public async Task<Equipment?> UpdateAsync(int id, Action<Equipment> updateAction)
    {
        var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment == null) return null;
        updateAction(equipment);
        equipment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return equipment;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment == null) return false;
        _context.Equipment.Remove(equipment);
        await _context.SaveChangesAsync();
        return true;
    }
}
