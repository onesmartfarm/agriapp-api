using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class GenerateInvoiceRequest
{
    [Required]
    public int WorkOrderId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    /// <summary>Required only when called by SuperUser to specify which center to bill under.</summary>
    public int? CenterId { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>Any additional charges on top of TotalMaterialCost before GST is applied.</summary>
    [Range(0, double.MaxValue)]
    public decimal AdditionalFees { get; set; }
}

public class IssueInvoiceRequest
{
    // Empty — issuing just transitions status from Draft → Issued
}

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public int CenterId { get; set; }
    public int WorkOrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "₹";
    public decimal BaseAmount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RecordPaymentRequest
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string TransactionReference { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public Guid Id { get; set; }
    public int CenterId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public string InvoiceStatus { get; set; } = string.Empty;
    public decimal InvoiceBalanceDue { get; set; }
}
