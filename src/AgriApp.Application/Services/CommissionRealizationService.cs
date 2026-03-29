using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class CommissionRealizationService
{
    private readonly AgriDbContext _db;

    public CommissionRealizationService(AgriDbContext db)
    {
        _db = db;
    }

    public async Task<(int Count, decimal TotalAmount)> ConfirmPaymentAsync(string upiTransactionId, int inquiryId)
    {
        if (string.IsNullOrWhiteSpace(upiTransactionId))
            throw new ArgumentException("UPI transaction id is required to realize commissions.", nameof(upiTransactionId));

        var pendingCommissions = await _db.CommissionLedgers
            .Where(c => c.InquiryId == inquiryId && c.Status == CommissionStatus.Pending)
            .ToListAsync();

        if (pendingCommissions.Count == 0)
            return (0, 0m);

        decimal totalAmount = 0m;
        foreach (var commission in pendingCommissions)
        {
            commission.Status = CommissionStatus.Realized;
            commission.UpiTransactionId = upiTransactionId;
            commission.UpdatedAt = DateTime.UtcNow;
            totalAmount += commission.Amount;
        }

        await _db.SaveChangesAsync();

        return (pendingCommissions.Count, totalAmount);
    }
}
