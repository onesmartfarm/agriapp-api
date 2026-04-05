using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using AgriApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class CustomerService
{
    private readonly CustomerRepository _repo;
    private readonly AgriDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CustomerService(CustomerRepository repo, AgriDbContext db, ICurrentUser currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CustomerResponse>> GetAllAsync()
    {
        var customers = await _repo.GetAllAsync();
        if (customers.Count == 0)
            return new List<CustomerResponse>();

        var pendingByKey = await GetPendingBalanceByCustomerCenterAsync(
            customers.Select(c => (c.Id, c.CenterId)).ToList());

        var centerIds = customers.Select(c => c.CenterId).Distinct().ToList();
        var symbols = await _db.Centers.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => centerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => string.IsNullOrWhiteSpace(c.CurrencySymbol) ? "₹" : c.CurrencySymbol);

        return customers.Select(c =>
        {
            pendingByKey.TryGetValue((c.Id, c.CenterId), out var bal);
            symbols.TryGetValue(c.CenterId, out var sym);
            return MapToResponse(c, bal, sym ?? "₹");
        }).ToList();
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Center)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
            return null;

        var pending = await GetPendingBalanceAsync(customer.Id, customer.CenterId);
        var sym = string.IsNullOrWhiteSpace(customer.Center?.CurrencySymbol)
            ? "₹"
            : customer.Center.CurrencySymbol;
        return MapToResponse(customer, pending, sym);
    }

    public async Task<CustomerFinancialSummaryResponse?> GetFinancialSummaryAsync(int customerId)
    {
        var customer = await _db.Customers.AsNoTracking().Include(c => c.Center).FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null)
            return null;

        var centerId = customer.CenterId;
        var invoiced = await _db.Invoices.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.CenterId == centerId)
            .SumAsync(i => i.TotalAmount);

        var paid = await (
            from p in _db.Payments.IgnoreQueryFilters().AsNoTracking()
            join inv in _db.Invoices.IgnoreQueryFilters().AsNoTracking() on p.InvoiceId equals inv.Id
            where inv.CustomerId == customerId && inv.CenterId == centerId
            select p.Amount
        ).SumAsync();

        var workOrderCount = await _db.WorkOrders
            .AsNoTracking()
            .CountAsync(w => w.CustomerId == customerId && w.CenterId == centerId);

        var sym = string.IsNullOrWhiteSpace(customer.Center?.CurrencySymbol)
            ? "₹"
            : customer.Center.CurrencySymbol;

        return new CustomerFinancialSummaryResponse
        {
            TotalWorkOrders = workOrderCount,
            TotalInvoiced = invoiced,
            TotalPaid = paid,
            PendingBalance = invoiced - paid,
            CurrencySymbol = sym
        };
    }

    public async Task<List<CustomerLedgerEntryResponse>> GetLedgerAsync(int customerId)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null)
            return new List<CustomerLedgerEntryResponse>();

        var centerId = customer.CenterId;

        var invoiceRows = await _db.Invoices.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.CenterId == centerId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.CreatedAt,
                i.TotalAmount,
                i.Id,
                i.WorkOrderId,
                i.Status
            })
            .ToListAsync();

        var invoices = invoiceRows.Select(i => new CustomerLedgerEntryResponse
        {
            Kind = "Invoice",
            OccurredAt = i.CreatedAt,
            Description = $"Invoice (work order #{i.WorkOrderId}) — {i.Status}",
            Amount = i.TotalAmount,
            InvoiceId = i.Id,
            PaymentId = null
        }).ToList();

        var paymentRows = await _db.Payments.IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                _db.Invoices.IgnoreQueryFilters().AsNoTracking(),
                p => p.InvoiceId,
                inv => inv.Id,
                (p, inv) => new { p, inv })
            .Where(x => x.inv.CustomerId == customerId && x.inv.CenterId == centerId)
            .OrderByDescending(x => x.p.PaymentDate)
            .Select(x => new
            {
                x.p.PaymentDate,
                x.p.Amount,
                x.p.TransactionReference,
                x.p.PaymentMethod,
                x.p.Id,
                InvoiceId = x.inv.Id
            })
            .ToListAsync();

        var payments = paymentRows.Select(x => new CustomerLedgerEntryResponse
        {
            Kind = "Payment",
            OccurredAt = x.PaymentDate,
            Description = string.IsNullOrEmpty(x.TransactionReference)
                ? $"Payment ({x.PaymentMethod})"
                : $"{x.PaymentMethod}: {x.TransactionReference}",
            Amount = x.Amount,
            InvoiceId = x.InvoiceId,
            PaymentId = x.Id
        }).ToList();

        return invoices
            .Concat(payments)
            .OrderByDescending(e => e.OccurredAt)
            .ToList();
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        int centerId;
        if (_currentUser.Role == Role.SuperUser)
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
            Notes = request.Notes,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.CreateAsync(customer);
        return await GetByIdAsync(created.Id)
            ?? throw new InvalidOperationException("Failed to load customer after create.");
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var updated = await _repo.UpdateAsync(id, customer =>
        {
            if (request.Name != null) customer.Name = request.Name;
            if (request.Phone != null) customer.Phone = request.Phone;
            if (request.Email != null) customer.Email = request.Email;
            if (request.Address != null) customer.Address = request.Address;
            if (request.Notes != null) customer.Notes = request.Notes;
        });
        return updated == null ? null : await GetByIdAsync(updated.Id);
    }

    public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

    private async Task<Dictionary<(int CustomerId, int CenterId), decimal>> GetPendingBalanceByCustomerCenterAsync(
        IReadOnlyList<(int Id, int CenterId)> customers)
    {
        var ids = customers.Select(c => c.Id).ToHashSet();
        if (ids.Count == 0)
            return new Dictionary<(int, int), decimal>();

        var invoicedRows = await _db.Invoices.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => ids.Contains(i.CustomerId))
            .GroupBy(i => new { i.CustomerId, i.CenterId })
            .Select(g => new { g.Key.CustomerId, g.Key.CenterId, Sum = g.Sum(x => x.TotalAmount) })
            .ToListAsync();
        var invoiced = invoicedRows.ToDictionary(x => (x.CustomerId, x.CenterId), x => x.Sum);

        var paidRows = await (
            from p in _db.Payments.IgnoreQueryFilters().AsNoTracking()
            join inv in _db.Invoices.IgnoreQueryFilters().AsNoTracking() on p.InvoiceId equals inv.Id
            where ids.Contains(inv.CustomerId)
            group p by new { inv.CustomerId, inv.CenterId } into g
            select new { g.Key.CustomerId, g.Key.CenterId, Sum = g.Sum(x => x.Amount) }
        ).ToListAsync();
        var paid = paidRows.ToDictionary(x => (x.CustomerId, x.CenterId), x => x.Sum);

        var result = new Dictionary<(int CustomerId, int CenterId), decimal>();
        foreach (var (cid, cenId) in customers)
        {
            var key = (cid, cenId);
            var inv = invoiced.GetValueOrDefault(key, 0m);
            var pay = paid.GetValueOrDefault(key, 0m);
            result[key] = inv - pay;
        }

        return result;
    }

    private async Task<decimal> GetPendingBalanceAsync(int customerId, int centerId)
    {
        var invoiced = await _db.Invoices.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.CenterId == centerId)
            .SumAsync(i => i.TotalAmount);

        var paid = await (
            from p in _db.Payments.IgnoreQueryFilters().AsNoTracking()
            join inv in _db.Invoices.IgnoreQueryFilters().AsNoTracking() on p.InvoiceId equals inv.Id
            where inv.CustomerId == customerId && inv.CenterId == centerId
            select p.Amount
        ).SumAsync();

        return invoiced - paid;
    }

    private static CustomerResponse MapToResponse(Customer c, decimal pendingBalance, string currencySymbol) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        Notes = c.Notes,
        CenterId = c.CenterId,
        CreatedAt = c.CreatedAt,
        PendingBalance = pendingBalance,
        CurrencySymbol = currencySymbol
    };
}
