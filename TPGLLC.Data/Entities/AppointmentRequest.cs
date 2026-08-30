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
    public string? VehicleSubmodel { get; set; }
    public string? BodyStyle { get; set; }
    public string? EngineFuel { get; set; }
    public string? Transmission { get; set; }
    public string? DriveType { get; set; }
    public string? Brake { get; set; }
    public string? Gvw { get; set; }

    [MaxLength(17)]
    public string? Vin { get; set; }

    public string? Mileage { get; set; }
    public string? LicensePlate { get; set; }
    public string? StateProvince { get; set; }
    public string? UnitNumber { get; set; }
    public string? FleetNumber { get; set; }
    public string? Color { get; set; }
    public string? VehicleMemo { get; set; }

    [Required]
    public string? PreferredDate { get; set; }

    [Required]
    public string? PreferredTime { get; set; }

    [Required]
    public string? ServiceNeeded { get; set; }

    [Required]
    public string? Message { get; set; }

    // Service advisor scheduling response fields.
    [MaxLength(20)]
    public string? ProposedDate { get; set; }
    [MaxLength(20)]
    public string? ProposedTime { get; set; }
    [MaxLength(4000)]
    public string? AdvisorMessage { get; set; }
    [MaxLength(128)]
    public string? ResponseToken { get; set; }
    public DateTimeOffset? ResponseTokenExpiresUtc { get; set; }

    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Pending";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool CanCustomerCancel { get; set; } = true;
}
