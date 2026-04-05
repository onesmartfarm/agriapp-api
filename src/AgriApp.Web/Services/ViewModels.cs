using System.ComponentModel.DataAnnotations;
using AgriApp.Core.Enums;

namespace AgriApp.Web.Services;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Role, int? CenterId);

/// <summary>List row for GET api/workorders. Status is the API string (e.g. Scheduled), not a numeric JSON enum.</summary>
public record WorkOrderListItem(
    int Id,
    string Description,
    string Status,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    string? ServiceActivityName,
    string? ImplementName,
    string? TractorName,
    decimal TotalMaterialCost,
    string CurrencySymbol);

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

// ── Users ─────────────────────────────────────────────────────────────────────

public record UserResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    int? CenterId);

// ── Equipment ────────────────────────────────────────────────────────────────

public record EquipmentResponse(
    int Id,
    string Name,
    string Category,
    decimal HourlyRate,
    int CenterId,
    string CenterName,
    string CurrencySymbol,
    int? VendorId,
    decimal? PurchaseCost,
    DateTime? PurchaseDate,
    bool IsImplement,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CustomerResponse(
    int Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    int CenterId,
    DateTime CreatedAt,
    decimal PendingBalance,
    string CurrencySymbol);

public record CustomerFinancialSummaryResponse(
    int TotalWorkOrders,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal PendingBalance,
    string CurrencySymbol);

public record CustomerLedgerEntryResponse(
    string Kind,
    DateTime OccurredAt,
    string Description,
    decimal Amount,
    Guid? InvoiceId,
    Guid? PaymentId);

public record VendorResponse(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    int CenterId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CenterResponse(
    int Id,
    string Name,
    string? Location,
    string CurrencySymbol,
    string TimeZoneId,
    int? AdminUserId);

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

public record ServiceActivityRow(
    int Id,
    string Name,
    string Description,
    decimal BaseRatePerHour,
    int CenterId,
    string CenterName,
    string CurrencySymbol,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateServiceActivityForm
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Rate must be positive")]
    public decimal BaseRatePerHour { get; set; } = 1m;

    /// <summary>0 until SuperUser picks a center or Manager claim is applied; avoids posting a bogus default of 1.</summary>
    public int CenterId { get; set; }
}

public record UpdateServiceActivityForm
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Rate must be positive")]
    public decimal? BaseRatePerHour { get; set; }
}

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

    public int CenterId { get; set; }
    public int? VendorId { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; }

    public bool IsImplement { get; set; }
}

public record UpdateEquipmentRequest
{
    [MaxLength(300)]
    public string? Name { get; set; }
    public string? Category { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be positive")]
    public decimal? HourlyRate { get; set; }
    public int? VendorId { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; }

    public bool? IsImplement { get; set; }
}

public record CreateCustomerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public int CenterId { get; set; }
}

public record UpdateCustomerRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public record CreateVendorRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? ContactPerson { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(320)]
    public string? Email { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }
    public int CenterId { get; set; }
}

// ── Inquiry ──────────────────────────────────────────────────────────────────

public record InquiryResponse(
    int Id,
    int CustomerId,
    string CustomerName,
    int EquipmentId,
    string EquipmentName,
    int SalespersonId,
    string SalespersonName,
    string Status,
    int CenterId,
    string CenterName,
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
    string CustomerName,
    string CenterName,
    string CurrencySymbol,
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

// ── Work Order ────────────────────────────────────────────────────────────────

public record WorkOrderTimeLog(
    int Id,
    DateTime StartTime,
    DateTime EndTime,
    string Type,
    string? Notes)
{
    public double Hours => EndTime > StartTime ? (EndTime - StartTime).TotalHours : 0;
    public bool IsBillable => Type == "Working";
}

public record WorkOrderDetail(
    int Id,
    int CenterId,
    int? CustomerId,
    int? InquiryId,
    int? ServiceActivityId,
    int? ImplementId,
    int? TractorId,
    int ResponsibleUserId,
    string Description,
    string Type,
    string Status,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    decimal TotalMaterialCost,
    string? ServiceActivityName,
    string? ImplementName,
    string? TractorName,
    string CurrencySymbol);

public record WorkOrderFormRequest
{
    [Required, Range(1, int.MaxValue, ErrorMessage = "Responsible User ID is required")]
    public int ResponsibleUserId { get; set; }

    public int? ServiceActivityId { get; set; }

    public int? ImplementId { get; set; }

    public int? TractorId { get; set; }

    public int? InquiryId { get; set; }

    public int? CenterId { get; set; }

    public int? CustomerId { get; set; }

    [Required, MaxLength(1000, ErrorMessage = "Description is too long")]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "RentalBooking";

    [Required]
    public DateTime ScheduledStartDate { get; set; } = DateTime.Now;

    [Required]
    public DateTime ScheduledEndDate { get; set; } = DateTime.Now.AddDays(1);

    [Range(0, double.MaxValue, ErrorMessage = "Cost cannot be negative")]
    public decimal TotalMaterialCost { get; set; }

    /// <summary>Timeline rows persisted server-side; invoice uses only Working-type logs for rental hours.</summary>
    public List<WorkOrderTimeLogRequest>? TimeLogs { get; set; }
}

public record WorkOrderTimeLogRequest(
    DateTime StartTime,
    DateTime EndTime,
    string LogType,
    string? Notes);

public record WorkOrderPatchRequest(int? CustomerId);

public record WorkOrderStatusUpdateRequest
{
    [Required]
    public string Status { get; set; } = "Scheduled";
}
