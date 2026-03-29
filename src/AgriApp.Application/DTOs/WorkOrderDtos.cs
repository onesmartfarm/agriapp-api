using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateWorkOrderRequest
{
    [Required]
    public int ResponsibleUserId { get; set; }

    public int? EquipmentId { get; set; }

    public int? InquiryId { get; set; }

    public int? CenterId { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledStartDate { get; set; }

    [Required]
    public DateTime ScheduledEndDate { get; set; }

    public decimal TotalMaterialCost { get; set; }
}

public class UpdateWorkOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class WorkOrderResponse
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public int? InquiryId { get; set; }
    public int? EquipmentId { get; set; }
    public int ResponsibleUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
