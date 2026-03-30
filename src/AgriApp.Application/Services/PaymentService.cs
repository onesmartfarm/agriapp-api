using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class PaymentService
{
    private readonly AgriDbContext _db;

    public PaymentService(AgriDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentResponse> RecordPaymentAsync(
        RecordPaymentRequest request, int centerId)
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var method))
            throw new ArgumentException(
                $"Invalid PaymentMethod: '{request.PaymentMethod}'. Valid values: UPI, BankTransfer, Cash.");

        var invoice = await _db.Invoices.FindAsync(request.InvoiceId)
            ?? throw new KeyNotFoundException($"Invoice {request.InvoiceId} not found.");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("This invoice is already fully paid.");

        if (invoice.Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Payment cannot be recorded against a Draft invoice. Issue it first.");

        var duplicate = await _db.Payments
            .AsNoTracking()
            .AnyAsync(p => p.TransactionReference == request.TransactionReference);

        if (duplicate)
            throw new InvalidOperationException(
                $"Transaction reference '{request.TransactionReference}' has already been recorded. Duplicate payment rejected.");

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            CenterId = centerId,
            InvoiceId = request.InvoiceId,
            Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = method,
            TransactionReference = request.TransactionReference,
            CreatedAt = DateTime.UtcNow
        };

        invoice.AmountPaid = Math.Round(
            invoice.AmountPaid + payment.Amount, 2, MidpointRounding.AwayFromZero);

        if (invoice.AmountPaid >= invoice.TotalAmount)
            invoice.Status = InvoiceStatus.Paid;
        else if (invoice.AmountPaid > 0)
            invoice.Status = InvoiceStatus.PartiallyPaid;

        invoice.UpdatedAt = DateTime.UtcNow;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return new PaymentResponse
        {
            Id = payment.Id,
            CenterId = payment.CenterId,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod.ToString(),
            TransactionReference = payment.TransactionReference,
            InvoiceStatus = invoice.Status.ToString(),
            InvoiceBalanceDue = Math.Round(
                invoice.TotalAmount - invoice.AmountPaid, 2, MidpointRounding.AwayFromZero)
        };
    }
}
