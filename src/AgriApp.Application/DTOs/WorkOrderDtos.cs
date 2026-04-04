using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateWorkOrderTimeLogDto
{
    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    /// <summary>Working, Break, or Breakdown (matches WorkTimeLogType enum).</summary>
    [Required]
    public string LogType { get; set; } = "Working";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class CreateWorkOrderRequest
{
    [Required]
    public int ResponsibleUserId { get; set; }

    public int? EquipmentId { get; set; }

    public int? InquiryId { get; set; }

    public int? CustomerId { get; set; }

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

    /// <summary>Optional timeline spans; invoice billable labor uses only Working entries.</summary>
    public List<CreateWorkOrderTimeLogDto>? TimeLogs { get; set; }
}

public class UpdateWorkOrderRequest
{
    /// <summary>Optional link to a customer in the same center; null clears the link.</summary>
    public int? CustomerId { get; set; }
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
    public int? CustomerId { get; set; }
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

    public string? EquipmentName { get; set; }
    public string CurrencySymbol { get; set; } = "₹";
}
