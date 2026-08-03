using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.Components.PortalShared.Vehicles;

public sealed class VehicleFormModel
{
    [Required(ErrorMessage = "Model year is required.")]
    public string? ModelYear { get; set; }

    [Required(ErrorMessage = "Make is required.")]
    public string? Make { get; set; }

    [Required(ErrorMessage = "Model is required.")]
    public string? Model { get; set; }

    [StringLength(100, ErrorMessage = "Nickname must be 100 characters or less.")]
    public string? Nickname { get; set; }

    [StringLength(20, ErrorMessage = "License plate must be 20 characters or less.")]
    public string? LicensePlate { get; set; }

    [StringLength(30, ErrorMessage = "VIN must be 30 characters or less.")]
    public string? Vin { get; set; }

    public string? Mileage { get; set; }

    public bool IsPrimary { get; set; }
}
