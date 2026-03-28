using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class CommissionLedger : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public int InquiryId { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public decimal Amount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public string? UpiTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Inquiry Inquiry { get; set; } = null!;
    public User User { get; set; } = null!;
    public Center Center { get; set; } = null!;
}
