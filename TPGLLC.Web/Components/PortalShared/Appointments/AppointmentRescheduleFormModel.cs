using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.Components.PortalShared.Appointments;

public sealed class AppointmentRescheduleFormModel
{
    [Required(ErrorMessage = "Preferred date is required.")]
    public string PreferredDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preferred time is required.")]
    public string PreferredTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Service needed is required.")]
    [StringLength(200, ErrorMessage = "Service needed cannot exceed 200 characters.")]
    public string ServiceNeeded { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Message cannot exceed 4000 characters.")]
    public string? Message { get; set; }
}
