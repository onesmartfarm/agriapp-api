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
