namespace TPGLLC.Data.Entities;

public sealed class CustomerVehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public string VehicleType { get; set; } = string.Empty; // Automotive, Motorcycle, Other

    public int? ModelYear { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? Vin { get; set; }

    public string? Nickname { get; set; }

    public string? LicensePlate { get; set; }

    public int? Mileage { get; set; }

    public bool IsPrimary { get; set; }

    public string? PhotoPath { get; set; }

    public DateTimeOffset? PhotoUpdatedUtc { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedUtc { get; set; }

    public Customer? Customer { get; set; }
}