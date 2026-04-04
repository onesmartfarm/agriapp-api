using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class WorkOrder : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public int? CustomerId { get; set; }
    public int? InquiryId { get; set; }
    public int? EquipmentId { get; set; }

    /// <summary>
    /// Maps to the existing 'StaffId' column in the database.
    /// </summary>
    public int ResponsibleUserId { get; set; }

    public string Description { get; set; } = string.Empty;
    public WorkOrderType Type { get; set; } = WorkOrderType.RentalBooking;
    public WorkStatus Status { get; set; } = WorkStatus.Scheduled;

    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public decimal TotalMaterialCost { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual Customer? Customer { get; set; }
    public Equipment? Equipment { get; set; }
    public User ResponsibleUser { get; set; } = null!;
    public Inquiry? Inquiry { get; set; }
    public ICollection<WorkOrderTimeLog> TimeLogs { get; set; } = new List<WorkOrderTimeLog>();
}
