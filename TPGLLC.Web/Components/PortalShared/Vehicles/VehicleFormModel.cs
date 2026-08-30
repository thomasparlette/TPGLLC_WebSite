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

    [StringLength(120)]
    public string? Submodel { get; set; }

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

    [StringLength(100, ErrorMessage = "Nickname must be 100 characters or less.")]
    public string? Nickname { get; set; }

    [StringLength(20, ErrorMessage = "License plate must be 20 characters or less.")]
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
    public string? Memo { get; set; }

    [StringLength(30, ErrorMessage = "VIN must be 30 characters or less.")]
    public string? Vin { get; set; }

    public string? Mileage { get; set; }

    public bool IsPrimary { get; set; }
}

public static class VehicleAttributeOptions
{
    public static IReadOnlyList<string> BodyStyles { get; } =
    [
        "Sedan", "Coupe", "Hatchback", "Wagon", "SUV", "Crossover",
        "Minivan", "Pickup", "Van", "Convertible", "Motorcycle", "Other"
    ];

    public static IReadOnlyList<string> DriveTypes { get; } =
        ["FWD", "RWD", "AWD", "4WD", "4x4", "Other"];

    public static IReadOnlyList<string> BrakeTypes { get; } =
        ["4-Wheel ABS", "2-Wheel ABS", "Rear ABS", "Disc", "Drum", "Other"];

    public static IReadOnlyList<string> CommonSubmodels { get; } =
    [
        "Base", "Sport", "Touring", "Limited", "L", "LE", "LX", "EX", "SE",
        "SL", "XLE", "XLT", "LT", "LS", "SLE", "SLT", "Denali", "SH-AWD"
    ];

    public static IReadOnlyList<string> CommonEngineFuelTypes { get; } =
    [
        "Gas", "Diesel", "Hybrid", "Electric", "2.0L I4", "2.5L I4", "3.5L V6",
        "5.0L V8", "5.3L V8", "6.2L V8"
    ];

    public static IReadOnlyList<string> CommonTransmissions { get; } =
    [
        "Automatic", "Manual", "CVT", "6-speed Automatic", "8-speed Automatic",
        "9-speed Automatic", "10-speed Automatic"
    ];
}
