using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Repositories;

namespace AgriApp.Application.Services;

public class InquiryService
{
    private readonly InquiryRepository _repo;
    private readonly ICurrentUser _currentUser;

    public InquiryService(InquiryRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
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

    public async Task<Inquiry> CreateAsync(int customerId, int equipmentId, int salespersonId, int centerId)
    {
        if (_currentUser.Role == Role.Sales && salespersonId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Sales users can only create inquiries for themselves.");

        var inquiry = new Inquiry
        {
            CustomerId = customerId,
            EquipmentId = equipmentId,
            SalespersonId = salespersonId,
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
            EquipmentId = i.EquipmentId,
            EquipmentName = i.Equipment?.Name ?? string.Empty,
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
