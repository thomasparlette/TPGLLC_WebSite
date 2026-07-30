namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public Guid? CustomerVehicleId { get; set; }

    public string VehicleName { get; set; } = string.Empty;
    public DateOnly ServiceDate { get; set; }
    public string Service { get; set; } = string.Empty;

    public int? Mileage { get; set; }
    public string? Technician { get; set; }

    public string Status { get; set; } = "Completed";
    public string? Notes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }

    public Customer? Customer { get; set; }
    public CustomerVehicle? Vehicle { get; set; }
}