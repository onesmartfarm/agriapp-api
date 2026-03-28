using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateWorkOrderRequest
{
    [Required]
    public int EquipmentId { get; set; }

    [Required]
    public int StaffId { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? CenterId { get; set; }
}

public class UpdateWorkOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
