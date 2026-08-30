namespace TPGLLC.Data.Entities;

public sealed class CustomerVehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public int? ModelYear { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? Submodel { get; set; }

    public string? BodyStyle { get; set; }

    public string? EngineFuel { get; set; }

    public string? Transmission { get; set; }

    public string? DriveType { get; set; }

    public string? Brake { get; set; }

    public string? Gvw { get; set; }

    public string? Vin { get; set; }

    public string? Nickname { get; set; }

    public string? LicensePlate { get; set; }

    public string? StateProvince { get; set; }

    public string? UnitNumber { get; set; }

    public string? FleetNumber { get; set; }

    public string? Color { get; set; }

    public string? Memo { get; set; }

    public int? Mileage { get; set; }

    public bool IsPrimary { get; set; }

    public string? PhotoPath { get; set; }

    public DateTimeOffset? PhotoUpdatedUtc { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedUtc { get; set; }

    public Customer? Customer { get; set; }
}
