using AgriApp.Core.Entities;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Infrastructure.Repositories;

public class CustomerRepository
{
    private readonly AgriDbContext _context;

    public CustomerRepository(AgriDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync()
        => await _context.Customers.AsNoTracking().ToListAsync();

    public async Task<Customer?> GetByIdAsync(int id)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> UpdateAsync(int id, Action<Customer> update)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return null;
        update(customer);
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return false;
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }
}
