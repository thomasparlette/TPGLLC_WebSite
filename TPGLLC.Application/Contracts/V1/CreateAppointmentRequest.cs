using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Application.Contracts.V1;

public sealed class CreateAppointmentRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? VehicleType { get; set; }

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

    public string? Company { get; set; }
}