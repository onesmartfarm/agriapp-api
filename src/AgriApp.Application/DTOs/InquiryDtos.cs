using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateInquiryRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int EquipmentId { get; set; }

    [Required]
    public int SalespersonId { get; set; }

    public int? CenterId { get; set; }
}

public class UpdateInquiryStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class InquiryApiResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public int SalespersonId { get; set; }
    public string SalespersonName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
