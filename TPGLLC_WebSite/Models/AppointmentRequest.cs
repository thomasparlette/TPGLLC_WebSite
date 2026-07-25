using System.ComponentModel.DataAnnotations;

namespace TPGLLC_WebSite.Models;

public sealed class AppointmentRequest
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Pending";

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public string? VehicleType { get; set; }

    [Required]
    public string? VehicleYear { get; set; }

    [Required]
    public string? VehicleMake { get; set; }

    [Required]
    public string? VehicleModel { get; set; }

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