using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using AgriApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class InquiryService
{
    private readonly InquiryRepository _repo;
    private readonly AgriDbContext _db;
    private readonly ICurrentUser _currentUser;

    public InquiryService(InquiryRepository repo, AgriDbContext db, ICurrentUser currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<InquiryApiResponse>> GetAllAsync()
    {
        var rows = await _repo.GetAllWithNavigationsAsync();
        var centerNames = await _repo.GetCenterNamesAsync(rows.Select(r => r.CenterId));
        return rows.Select(i => ToApiResponse(i, centerNames)).ToList();
    }

    public async Task<InquiryApiResponse?> GetByIdAsync(int id)
    {
        var inquiry = await _repo.GetByIdWithNavigationsAsync(id);
        if (inquiry == null) return null;
        var centerNames = await _repo.GetCenterNamesAsync(new[] { inquiry.CenterId });
        return ToApiResponse(inquiry, centerNames);
    }

    public async Task<List<InquiryApiResponse>> GetByCustomerIdAsync(int customerId)
    {
        var rows = await _repo.GetByCustomerIdWithNavigationsAsync(customerId);
        if (rows.Count == 0)
            return new List<InquiryApiResponse>();
        var centerNames = await _repo.GetCenterNamesAsync(rows.Select(r => r.CenterId));
        return rows.Select(i => ToApiResponse(i, centerNames)).ToList();
    }

    public async Task<Inquiry> CreateAsync(CreateInquiryRequest request, int centerId)
    {
        if (_currentUser.Role == Role.Sales && request.SalespersonId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Sales users can only create inquiries for themselves.");

        var activity = await _db.ServiceActivities.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ServiceActivityId && a.CenterId == centerId);
        if (activity == null)
            throw new InvalidOperationException("Service activity not found for this center.");

        if (request.EquipmentId is int eqId)
        {
            var eq = await _db.Equipment.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eqId && e.CenterId == centerId);
            if (eq == null)
                throw new InvalidOperationException("Equipment not found for this center.");
        }

        var customerOk = await _db.Customers.AsNoTracking()
            .AnyAsync(c => c.Id == request.CustomerId && c.CenterId == centerId);
        if (!customerOk)
            throw new InvalidOperationException("Customer not found for this center.");

        var inquiry = new Inquiry
        {
            CustomerId = request.CustomerId,
            ServiceActivityId = request.ServiceActivityId,
            EquipmentId = request.EquipmentId,
            SalespersonId = request.SalespersonId,
            CenterId = centerId,
            Status = InquiryStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        return await _repo.CreateAsync(inquiry);
    }

    public async Task<Inquiry?> UpdateStatusAsync(int id, InquiryStatus status)
        => await _repo.UpdateStatusAsync(id, status);

    private static InquiryApiResponse ToApiResponse(Inquiry i, IReadOnlyDictionary<int, string> centerNames)
    {
        centerNames.TryGetValue(i.CenterId, out var centerName);
        return new InquiryApiResponse
        {
            Id = i.Id,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name ?? string.Empty,
            ServiceActivityId = i.ServiceActivityId,
            ServiceActivityName = i.ServiceActivity?.Name,
            EquipmentId = i.EquipmentId,
            EquipmentName = i.Equipment?.Name,
            SalespersonId = i.SalespersonId,
            SalespersonName = i.Salesperson?.Name ?? string.Empty,
            Status = i.Status.ToString(),
            CenterId = i.CenterId,
            CenterName = centerName ?? string.Empty,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        };
    }
}
