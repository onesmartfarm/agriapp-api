using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateCustomerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(320), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public int? CenterId { get; set; }
}

public class UpdateCustomerRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(320), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class CustomerResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Sum(invoice TotalAmount) − Sum(payment Amount) for this customer and center.</summary>
    public decimal PendingBalance { get; set; }

    public string CurrencySymbol { get; set; } = "₹";
}

public class CustomerFinancialSummaryResponse
{
    public int TotalWorkOrders { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingBalance { get; set; }
    public string CurrencySymbol { get; set; } = "₹";
}

public class CustomerLedgerEntryResponse
{
    public string Kind { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
}
