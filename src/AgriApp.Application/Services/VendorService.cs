using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Repositories;

namespace AgriApp.Application.Services;

public class VendorService
{
    private readonly VendorRepository _repo;
    private readonly ICurrentUser _currentUser;

    public VendorService(VendorRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<VendorResponse>> GetAllAsync()
    {
        var vendors = await _repo.GetAllAsync();
        return vendors.Select(MapToResponse).ToList();
    }

    public async Task<VendorResponse?> GetByIdAsync(int id)
    {
        var vendor = await _repo.GetByIdAsync(id);
        return vendor == null ? null : MapToResponse(vendor);
    }

    public async Task<VendorResponse> CreateAsync(CreateVendorRequest request)
    {
        int centerId;
        if (_currentUser.Role == Core.Enums.Role.SuperUser)
            centerId = request.CenterId ?? _currentUser.CenterId
                ?? throw new InvalidOperationException("CenterId is required.");
        else
            centerId = _currentUser.CenterId
                ?? throw new InvalidOperationException("User must belong to a center.");

        var vendor = new Vendor
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.CreateAsync(vendor);
        return MapToResponse(created);
    }

    public async Task<VendorResponse?> UpdateAsync(int id, UpdateVendorRequest request)
    {
        var updated = await _repo.UpdateAsync(id, vendor =>
        {
            if (request.Name != null) vendor.Name = request.Name;
            if (request.ContactPerson != null) vendor.ContactPerson = request.ContactPerson;
            if (request.Phone != null) vendor.Phone = request.Phone;
            if (request.Email != null) vendor.Email = request.Email;
            if (request.Address != null) vendor.Address = request.Address;
        });
        return updated == null ? null : MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

    private static VendorResponse MapToResponse(Vendor v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        ContactPerson = v.ContactPerson,
        Phone = v.Phone,
        Email = v.Email,
        Address = v.Address,
        CenterId = v.CenterId,
        CreatedAt = v.CreatedAt
    };
}
