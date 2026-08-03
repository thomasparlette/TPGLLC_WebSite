using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.Components.PortalShared.Appointments;

public sealed class AppointmentRequestFormModel
{
    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    public string Phone { get; set; } = string.Empty;

    public string? VehicleYear { get; set; }

    public string? VehicleMake { get; set; }

    public string? VehicleModel { get; set; }

    public string? Vin { get; set; }

    public string? Mileage { get; set; }

    [Required(ErrorMessage = "Service needed is required.")]
    public string ServiceNeeded { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preferred date is required.")]
    public string PreferredDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preferred time is required.")]
    public string PreferredTime { get; set; } = string.Empty;

    public string? Message { get; set; }
}