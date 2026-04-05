using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Infrastructure.Repositories;

public class InquiryRepository
{
    private readonly AgriDbContext _context;

    public InquiryRepository(AgriDbContext context)
    {
        _context = context;
    }

    public async Task<List<Inquiry>> GetAllAsync()
        => await _context.Inquiries.AsNoTracking().ToListAsync();

    public async Task<List<Inquiry>> GetAllWithNavigationsAsync()
        => await _context.Inquiries
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Equipment)
            .Include(i => i.Salesperson)
            .ToListAsync();

    public async Task<List<Inquiry>> GetByCustomerIdWithNavigationsAsync(int customerId)
        => await _context.Inquiries
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Equipment)
            .Include(i => i.Salesperson)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.Id)
            .ToListAsync();

    public async Task<Dictionary<int, string>> GetCenterNamesAsync(IEnumerable<int> centerIds)
    {
        var ids = centerIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        return await _context.Centers
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);
    }

    public async Task<Inquiry?> GetByIdAsync(int id)
        => await _context.Inquiries.FindAsync(id);

    public async Task<Inquiry?> GetByIdWithNavigationsAsync(int id)
        => await _context.Inquiries
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Equipment)
            .Include(i => i.Salesperson)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Inquiry> CreateAsync(Inquiry inquiry)
    {
        _context.Inquiries.Add(inquiry);
        await _context.SaveChangesAsync();
        return inquiry;
    }

    public async Task<Inquiry?> UpdateStatusAsync(int id, InquiryStatus status)
    {
        var inquiry = await _context.Inquiries.FindAsync(id);
        if (inquiry == null) return null;
        inquiry.Status = status;
        inquiry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return inquiry;
    }
}
