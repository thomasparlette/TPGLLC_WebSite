using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.Components.PortalShared.Appointments;

public sealed class AppointmentRequestFormModel
{
    [Required(ErrorMessage = "Please select a vehicle year.")]
    public string? VehicleYear { get; set; }

    [Required(ErrorMessage = "Please select a vehicle make.")]
    public string? VehicleMake { get; set; }

    [Required(ErrorMessage = "Please select a vehicle model.")]
    public string? VehicleModel { get; set; }

    [StringLength(120)]
    public string? VehicleSubmodel { get; set; }

    [StringLength(80)]
    public string? BodyStyle { get; set; }

    [StringLength(160)]
    public string? EngineFuel { get; set; }

    [StringLength(120)]
    public string? Transmission { get; set; }

    [StringLength(60)]
    public string? DriveType { get; set; }

    [StringLength(80)]
    public string? Brake { get; set; }

    [StringLength(40)]
    public string? Gvw { get; set; }

    [StringLength(17, ErrorMessage = "VIN cannot exceed 17 characters.")]
    public string? Vin { get; set; }

    [RegularExpression(@"^\d*$", ErrorMessage = "Mileage must contain numbers only.")]
    public string? Mileage { get; set; }

    [StringLength(25)]
    public string? LicensePlate { get; set; }

    [StringLength(50)]
    public string? StateProvince { get; set; }

    [StringLength(50)]
    public string? UnitNumber { get; set; }

    [StringLength(50)]
    public string? FleetNumber { get; set; }

    [StringLength(60)]
    public string? Color { get; set; }

    [StringLength(2000)]
    public string? VehicleMemo { get; set; }

    [Required(ErrorMessage = "Please describe the service requested.")]
    [StringLength(200, ErrorMessage = "Service Needed cannot exceed 200 characters.")]
    public string ServiceNeeded { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a preferred date.")]
    public string PreferredDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a preferred time.")]
    public string PreferredTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(25)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please tell us what you need.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}
