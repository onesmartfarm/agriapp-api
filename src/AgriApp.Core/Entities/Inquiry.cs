using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Inquiry : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? ServiceActivityId { get; set; }
    /// <summary>Optional specific asset the customer asked for (implement or tractor).</summary>
    public int? EquipmentId { get; set; }
    public int SalespersonId { get; set; }
    public InquiryStatus Status { get; set; } = InquiryStatus.New;
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public ServiceActivity? ServiceActivity { get; set; }
    public Equipment? Equipment { get; set; }
    public User Salesperson { get; set; } = null!;
}
