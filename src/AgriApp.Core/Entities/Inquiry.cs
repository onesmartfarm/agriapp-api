using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Inquiry : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int EquipmentId { get; set; }
    public int SalespersonId { get; set; }
    public InquiryStatus Status { get; set; } = InquiryStatus.New;
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Customer { get; set; } = null!;
    public Equipment Equipment { get; set; } = null!;
    public User Salesperson { get; set; } = null!;
}
