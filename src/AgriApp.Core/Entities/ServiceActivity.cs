using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class ServiceActivity : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaseRatePerHour { get; set; }
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Center Center { get; set; } = null!;
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    public ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
}
