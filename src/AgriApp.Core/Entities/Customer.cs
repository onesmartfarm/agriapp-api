using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Customer : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Center Center { get; set; } = null!;
    public ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
