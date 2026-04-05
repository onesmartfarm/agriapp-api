using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

/// <summary>
/// Records time spans within a Work Order (Working, Break, Breakdown).
/// Invoice calculations use ONLY "Working" spans, excluding Break and Breakdown.
/// </summary>
public class WorkOrderTimeLog : IAuditable
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Type of time span: Working, Break, or Breakdown.
    /// Only "Working" spans count towards billable hours.
    /// </summary>
    public WorkTimeLogType LogType { get; set; } = WorkTimeLogType.Working;
    
    /// <summary>
    /// Optional notes (e.g., reason for breakdown, type of break).
    /// </summary>
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public WorkOrder WorkOrder { get; set; } = null!;
}
