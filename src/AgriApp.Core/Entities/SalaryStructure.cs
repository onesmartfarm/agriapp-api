using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class SalaryStructure : ICenterScoped, IAuditable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Center Center { get; set; } = null!;
}
