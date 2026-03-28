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

    public async Task<List<Inquiry>> GetAllAsync()
        => await _repo.GetAllAsync();

    public async Task<Inquiry?> GetByIdAsync(int id)
        => await _repo.GetByIdAsync(id);

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
}
