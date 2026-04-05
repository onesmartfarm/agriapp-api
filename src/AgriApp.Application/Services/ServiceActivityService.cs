using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class ServiceActivityService
{
    private readonly AgriDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ServiceActivityService(AgriDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ServiceActivityResponse>> GetAllAsync(int? filterCenterId = null)
    {
        var q = _db.ServiceActivities.AsNoTracking().Include(a => a.Center).AsQueryable();

        if (_currentUser.Role != Role.SuperUser)
        {
            var myCenter = _currentUser.CenterId
                ?? throw new InvalidOperationException("User must belong to a center.");
            q = q.Where(a => a.CenterId == myCenter);
        }
        else if (filterCenterId is int cid)
        {
            q = q.Where(a => a.CenterId == cid);
        }

        var rows = await q.OrderBy(a => a.Name).ToListAsync();
        return rows.Select(ToResponse).ToList();
    }

    public async Task<ServiceActivityResponse?> GetByIdAsync(int id)
    {
        var a = await _db.ServiceActivities
            .AsNoTracking()
            .Include(x => x.Center)
            .FirstOrDefaultAsync(x => x.Id == id);
        return a == null ? null : ToResponse(a);
    }

    public async Task<ServiceActivityResponse> CreateAsync(CreateServiceActivityRequest request)
    {
        var centerId = ResolveWriteCenterId(request.CenterId);
        await EnsureCenterExistsAsync(centerId);

        var entity = new ServiceActivity
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            BaseRatePerHour = request.BaseRatePerHour,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ServiceActivities.Add(entity);
        await _db.SaveChangesAsync();

        var reloaded = await _db.ServiceActivities
            .AsNoTracking()
            .Include(a => a.Center)
            .FirstAsync(a => a.Id == entity.Id);
        return ToResponse(reloaded);
    }

    public async Task<ServiceActivityResponse?> UpdateAsync(int id, UpdateServiceActivityRequest request)
    {
        var entity = await _db.ServiceActivities.FirstOrDefaultAsync(a => a.Id == id);
        if (entity == null) return null;

        if (request.Name != null) entity.Name = request.Name;
        if (request.Description != null) entity.Description = request.Description;
        if (request.BaseRatePerHour.HasValue) entity.BaseRatePerHour = request.BaseRatePerHour.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.ServiceActivities.FirstOrDefaultAsync(a => a.Id == id);
        if (entity == null) return false;
        _db.ServiceActivities.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private int ResolveWriteCenterId(int requestCenterId)
    {
        if (_currentUser.Role == Role.SuperUser)
            return requestCenterId;

        var myCenter = _currentUser.CenterId
            ?? throw new InvalidOperationException("User must belong to a center.");
        if (requestCenterId != myCenter)
            throw new ArgumentException("CenterId does not match your assigned center.");
        return myCenter;
    }

    private async Task EnsureCenterExistsAsync(int centerId)
    {
        var ok = await _db.Centers.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(c => c.Id == centerId);
        if (!ok)
            throw new ArgumentException(
                $"No center exists with id {centerId}. For managers, ensure your account’s CenterId matches a row in the database; for SuperUser, pick a center from the list.");
    }

    private static ServiceActivityResponse ToResponse(ServiceActivity a)
    {
        var sym = string.IsNullOrWhiteSpace(a.Center?.CurrencySymbol) ? "₹" : a.Center.CurrencySymbol;
        return new ServiceActivityResponse
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            BaseRatePerHour = a.BaseRatePerHour,
            CenterId = a.CenterId,
            CenterName = a.Center?.Name ?? string.Empty,
            CurrencySymbol = sym,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
