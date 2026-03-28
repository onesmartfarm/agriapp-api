using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class WorkOrder : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public int StaffId { get; set; }
    public string Description { get; set; } = string.Empty;
    public WorkStatus Status { get; set; } = WorkStatus.Pending;
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Equipment Equipment { get; set; } = null!;
    public User Staff { get; set; } = null!;
}
