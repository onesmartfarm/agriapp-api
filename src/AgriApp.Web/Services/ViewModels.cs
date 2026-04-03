using System.ComponentModel.DataAnnotations;
using AgriApp.Core.Enums;

namespace AgriApp.Web.Services;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Role, int? CenterId);

public record WorkOrderListItem(
    int Id,
    string Description,
    WorkStatus Status,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    string? EquipmentName,
    decimal TotalMaterialCost);

public record AttendanceClockRequest(
    string Action,
    double? Latitude,
    double? Longitude);

public record AttendanceResponse(
    int Id,
    string Action,
    DateTime Timestamp,
    double? Latitude,
    double? Longitude);

// ── Equipment ────────────────────────────────────────────────────────────────

public record EquipmentResponse(
    int Id,
    string Name,
    string Category,
    decimal HourlyRate,
    int CenterId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record QuoteGstBreakdown(
    decimal BaseAmount,
    decimal Cgst,
    decimal Sgst,
    decimal TotalGst,
    decimal GrandTotal);

public record QuoteCommissionResult(
    decimal RentalAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal NetToCompany);

public record EquipmentQuoteResult(
    decimal Hours,
    QuoteGstBreakdown Pricing,
    QuoteCommissionResult Commission);

public record CreateEquipmentRequest
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = "Tractor";

    [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be positive")]
    public decimal HourlyRate { get; set; } = 1.0m;

    public int CenterId { get; set; } = 1;
}

public record UpdateEquipmentRequest
{
    [MaxLength(300)]
    public string? Name { get; set; }
    public string? Category { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be positive")]
    public decimal? HourlyRate { get; set; }
}

// ── Inquiry ──────────────────────────────────────────────────────────────────

public record InquiryResponse(
    int Id,
    int CustomerId,
    int EquipmentId,
    int SalespersonId,
    string Status,
    int CenterId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateInquiryRequest
{
    [Required, Range(1, int.MaxValue, ErrorMessage = "Customer ID is required")]
    public int CustomerId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Equipment ID is required")]
    public int EquipmentId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Salesperson ID is required")]
    public int SalespersonId { get; set; }

    public int? CenterId { get; set; }
}

public record UpdateInquiryStatusRequest
{
    [Required]
    public string Status { get; set; } = "New";
}

// ── Invoice ──────────────────────────────────────────────────────────────────

public record InvoiceResponse(
    Guid Id,
    int CenterId,
    int WorkOrderId,
    int CustomerId,
    decimal BaseAmount,
    decimal GstAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    DateTime DueDate,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record GenerateInvoiceRequest
{
    [Required, Range(1, int.MaxValue, ErrorMessage = "Work Order ID is required")]
    public int WorkOrderId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Customer ID is required")]
    public int CustomerId { get; set; }

    public int? CenterId { get; set; }

    [Required]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Range(0, double.MaxValue, ErrorMessage = "Additional fees cannot be negative")]
    public decimal AdditionalFees { get; set; }
}

// ── Payment ──────────────────────────────────────────────────────────────────

public record PaymentRecordRequest
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "UPI";

    [Required, MaxLength(200)]
    public string TransactionReference { get; set; } = string.Empty;
}

public record PaymentResultResponse(
    Guid Id,
    int CenterId,
    Guid InvoiceId,
    decimal Amount,
    DateTime PaymentDate,
    string PaymentMethod,
    string TransactionReference,
    string InvoiceStatus,
    decimal InvoiceBalanceDue);
