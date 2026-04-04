using AgriApp.Core.Entities;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Infrastructure.Repositories;

public class VendorRepository
{
    private readonly AgriDbContext _context;

    public VendorRepository(AgriDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vendor>> GetAllAsync()
        => await _context.Vendors.AsNoTracking().ToListAsync();

    public async Task<Vendor?> GetByIdAsync(int id)
        => await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Vendor> CreateAsync(Vendor vendor)
    {
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();
        return vendor;
    }

    public async Task<Vendor?> UpdateAsync(int id, Action<Vendor> update)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null) return null;
        update(vendor);
        vendor.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return vendor;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null) return false;
        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
        return true;
    }
}
