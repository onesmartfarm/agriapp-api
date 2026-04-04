using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Repositories;

namespace AgriApp.Application.Services;

public class CustomerService
{
    private readonly CustomerRepository _repo;
    private readonly ICurrentUser _currentUser;

    public CustomerService(CustomerRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<CustomerResponse>> GetAllAsync()
    {
        var customers = await _repo.GetAllAsync();
        return customers.Select(MapToResponse).ToList();
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        var customer = await _repo.GetByIdAsync(id);
        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        int centerId;
        if (_currentUser.Role == Core.Enums.Role.SuperUser)
            centerId = request.CenterId ?? _currentUser.CenterId
                ?? throw new InvalidOperationException("CenterId is required.");
        else
            centerId = _currentUser.CenterId
                ?? throw new InvalidOperationException("User must belong to a center.");

        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.CreateAsync(customer);
        return MapToResponse(created);
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var updated = await _repo.UpdateAsync(id, customer =>
        {
            if (request.Name != null) customer.Name = request.Name;
            if (request.Phone != null) customer.Phone = request.Phone;
            if (request.Email != null) customer.Email = request.Email;
            if (request.Address != null) customer.Address = request.Address;
        });
        return updated == null ? null : MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        CenterId = c.CenterId,
        CreatedAt = c.CreatedAt
    };
}
