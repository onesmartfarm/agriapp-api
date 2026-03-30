using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class InvoiceService
{
    private readonly AgriDbContext _db;

    public InvoiceService(AgriDbContext db)
    {
        _db = db;
    }

    public async Task<List<InvoiceResponse>> GetAllAsync()
        => await _db.Invoices
            .AsNoTracking()
            .Select(i => MapToResponse(i))
            .ToListAsync();

    public async Task<InvoiceResponse?> GetByIdAsync(Guid id)
    {
        var invoice = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return invoice == null ? null : MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> GenerateInvoiceFromWorkOrderAsync(
        GenerateInvoiceRequest request, int centerId)
    {
        var workOrder = await _db.WorkOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkOrderId)
            ?? throw new KeyNotFoundException($"WorkOrder {request.WorkOrderId} not found.");

        if (workOrder.Status != WorkStatus.Completed)
            throw new InvalidOperationException(
                $"Invoice can only be generated for Completed work orders. Current status: {workOrder.Status}.");

        var existing = await _db.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.WorkOrderId == request.WorkOrderId);

        if (existing)
            throw new InvalidOperationException(
                $"An invoice has already been generated for WorkOrder {request.WorkOrderId}.");

        var baseAmount = Math.Round(
            workOrder.TotalMaterialCost + request.AdditionalFees, 2, MidpointRounding.AwayFromZero);

        var gst = GstCalculator.Calculate(baseAmount);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CenterId = centerId,
            WorkOrderId = request.WorkOrderId,
            CustomerId = request.CustomerId,
            BaseAmount = gst.BaseAmount,
            GstAmount = gst.TotalGst,
            TotalAmount = gst.GrandTotal,
            AmountPaid = 0m,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse?> IssueAsync(Guid id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice == null) return null;

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Only Draft invoices can be issued. Current status: {invoice.Status}.");

        invoice.Status = InvoiceStatus.Issued;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToResponse(invoice);
    }

    public async Task MarkOverdueAsync()
    {
        var overdue = await _db.Invoices
            .Where(i =>
                (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) &&
                i.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invoice in overdue)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.UpdatedAt = DateTime.UtcNow;
        }

        if (overdue.Count > 0)
            await _db.SaveChangesAsync();
    }

    internal static InvoiceResponse MapToResponse(Invoice i) => new()
    {
        Id = i.Id,
        CenterId = i.CenterId,
        WorkOrderId = i.WorkOrderId,
        CustomerId = i.CustomerId,
        BaseAmount = i.BaseAmount,
        GstAmount = i.GstAmount,
        TotalAmount = i.TotalAmount,
        AmountPaid = i.AmountPaid,
        BalanceDue = Math.Round(i.TotalAmount - i.AmountPaid, 2, MidpointRounding.AwayFromZero),
        DueDate = i.DueDate,
        Status = i.Status.ToString(),
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt
    };
}
