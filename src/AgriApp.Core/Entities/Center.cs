using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Center : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
