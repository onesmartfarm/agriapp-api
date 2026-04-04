using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Vendor : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Center Center { get; set; } = null!;
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
