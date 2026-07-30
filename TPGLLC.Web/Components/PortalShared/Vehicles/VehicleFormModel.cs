using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.Components.PortalShared.Vehicles;

public sealed class VehicleFormModel
{
    [Required(ErrorMessage = "Model year is required.")]
    public string ModelYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "Make is required.")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    public string Model { get; set; } = string.Empty;

    public string? Nickname { get; set; }

    public string? LicensePlate { get; set; }

    public string? Vin { get; set; }

    public string? Mileage { get; set; }

    public bool IsPrimary { get; set; }
}