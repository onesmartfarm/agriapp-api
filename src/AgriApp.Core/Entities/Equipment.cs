using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Equipment : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EquipmentCategory Category { get; set; }
    public decimal HourlyRate { get; set; }
    public int CenterId { get; set; }
    public int? VendorId { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Center Center { get; set; } = null!;
    public Vendor? Vendor { get; set; }
    public ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
