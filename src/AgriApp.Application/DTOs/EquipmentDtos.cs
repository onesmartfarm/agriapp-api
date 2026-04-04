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

    public int? VendorId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PurchaseCost { get; set; }

    public DateTime? PurchaseDate { get; set; }
}

public class UpdateEquipmentRequest
{
    [MaxLength(300)]
    public string? Name { get; set; }
    public string? Category { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "HourlyRate must be positive")]
    public decimal? HourlyRate { get; set; }

    public int? VendorId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PurchaseCost { get; set; }

    public DateTime? PurchaseDate { get; set; }
}

public class RentalQuoteRequest
{
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Hours must be positive")]
    public decimal Hours { get; set; }
}

/// <summary>Equipment row for API/Web with center display fields.</summary>
public class EquipmentApiResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "₹";
    public int? VendorId { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
