using System.ComponentModel.DataAnnotations;

namespace AgriApp.Application.DTOs;

public class CreateVendorRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(320), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public int? CenterId { get; set; }
}

public class UpdateVendorRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(320), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}

public class VendorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int CenterId { get; set; }
    public DateTime CreatedAt { get; set; }
}
