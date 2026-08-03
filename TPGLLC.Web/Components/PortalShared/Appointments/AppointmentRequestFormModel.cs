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

    public string VehicleType { get; set; } = "Automotive";

    [Required(ErrorMessage = "Vehicle year is required.")]
    public string? VehicleYear { get; set; }

    [Required(ErrorMessage = "Vehicle make is required.")]
    public string? VehicleMake { get; set; }

    [Required(ErrorMessage = "Vehicle model is required.")]
    public string? VehicleModel { get; set; }

    public string? Vin { get; set; }

    public string? Mileage { get; set; }

    [Required(ErrorMessage = "Service needed is required.")]
    public string ServiceNeeded { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preferred date is required.")]
    public string PreferredDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preferred time is required.")]
    public string PreferredTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    public string? Message { get; set; }
}