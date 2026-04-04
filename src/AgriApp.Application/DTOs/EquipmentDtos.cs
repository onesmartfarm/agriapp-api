using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateEquipmentRequest
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "HourlyRate must be positive")]
    public decimal HourlyRate { get; set; }

    [Required]
    public int CenterId { get; set; }
}

public class UpdateEquipmentRequest
{
    [MaxLength(300)]
    public string? Name { get; set; }
    public string? Category { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "HourlyRate must be positive")]
    public decimal? HourlyRate { get; set; }
}

public class RentalQuoteRequest
{
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Hours must be positive")]
    public decimal Hours { get; set; }
}
