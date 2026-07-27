namespace TPGLLC.Shared.Models;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? ApplicationUserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedUtc { get; set; }

    public ICollection<CustomerVehicle> Vehicles { get; set; } = new List<CustomerVehicle>();
}