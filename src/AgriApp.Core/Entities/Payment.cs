using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Payment : ICenterScoped, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK → centers.Id (int, matches existing PK type)</summary>
    public int CenterId { get; set; }

    /// <summary>FK → invoices.Id (Guid)</summary>
    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>UPI transaction ID or bank reference. Unique to prevent duplicate payments.</summary>
    public string TransactionReference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
