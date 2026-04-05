using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateServiceActivityRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Base rate must be positive")]
    public decimal BaseRatePerHour { get; set; }

    /// <summary>Must match an existing center row. Range rejects 0; [Required] alone does not reject default int.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "A valid center id is required.")]
    public int CenterId { get; set; }
}

public class UpdateServiceActivityRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Base rate must be positive")]
    public decimal? BaseRatePerHour { get; set; }
}

public class ServiceActivityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaseRatePerHour { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "₹";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
