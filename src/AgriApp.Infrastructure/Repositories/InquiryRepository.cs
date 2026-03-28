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

    public async Task<Inquiry?> GetByIdAsync(int id)
        => await _context.Inquiries.FindAsync(id);

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
