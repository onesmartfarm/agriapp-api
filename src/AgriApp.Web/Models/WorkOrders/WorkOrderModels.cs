using System.ComponentModel.DataAnnotations;
using AgriApp.Core.Enums;

namespace AgriApp.Web.Models.WorkOrders;

public class WorkOrderDto
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public int? EquipmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWorkOrderRequest
{
    [Required]
    public int EquipmentId { get; set; }

    public int? InquiryId { get; set; }

    [Required]
    public int ResponsibleUserId { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public WorkOrderType Type { get; set; }

    [Required]
    public DateTime ScheduledStartDate { get; set; }

    [Required]
    public DateTime ScheduledEndDate { get; set; }
}
