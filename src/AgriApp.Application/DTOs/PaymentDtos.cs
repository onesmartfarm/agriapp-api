using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class PaymentWebhookRequest
{
    [Required]
    public string UpiTransactionId { get; set; } = string.Empty;

    [Required]
    public int InquiryId { get; set; }
}

public class PaymentWebhookResponse
{
    public string UpiTransactionId { get; set; } = string.Empty;
    public int InquiryId { get; set; }
    public int CommissionsRealized { get; set; }
    public decimal TotalAmountRealized { get; set; }
}

public class CommissionLedgerResponse
{
    public int Id { get; set; }
    public int InquiryId { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UpiTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SalaryStructureRequest
{
    [Required]
    public int UserId { get; set; }

    public int? CenterId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BaseSalary { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal CommissionPercentage { get; set; }
}

public class SalaryStructureResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionPercentage { get; set; }
}
