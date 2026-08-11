using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Data.Entities;

public sealed class AppointmentRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public string? VehicleYear { get; set; }
    public string? VehicleMake { get; set; }
    public string? VehicleModel { get; set; }

    [MaxLength(17)]
    public string? Vin { get; set; }

    public string? Mileage { get; set; }

    [Required]
    public string? PreferredDate { get; set; }

    [Required]
    public string? PreferredTime { get; set; }

    [Required]
    public string? ServiceNeeded { get; set; }

    [Required]
    public string? Message { get; set; }


    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Pending";

    public string? ProposedDate { get; set; }
    public string? ProposedTime { get; set; }
    public string? AdvisorMessage { get; set; }

    [MaxLength(128)]
    public string? ResponseToken { get; set; }
    public DateTimeOffset? ResponseTokenExpiresUtc { get; set; }
}