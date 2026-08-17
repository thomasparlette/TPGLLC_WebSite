using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Customers;

namespace TPGLLC.Web.Services.Portal;

public sealed class PortalContextViewModel
{
    public CurrentCustomer CurrentCustomer { get; init; } = new();

    public CustomerProfile? Profile { get; init; }

    public Customer? Customer { get; init; }

    public IReadOnlyList<CustomerVehicle> Vehicles { get; init; } = Array.Empty<CustomerVehicle>();

    public IReadOnlyList<ServiceHistoryEntry> ServiceHistoryEntries { get; init; } = Array.Empty<ServiceHistoryEntry>();

    public IReadOnlyList<AppointmentRequest> AppointmentRequests { get; init; } = Array.Empty<AppointmentRequest>();

    public CustomerVehicle? PrimaryVehicle =>Vehicles.FirstOrDefault(x => x.IsPrimary);

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(CurrentCustomer.DisplayName)
            ? CurrentCustomer.DisplayName.Trim()
            : !string.IsNullOrWhiteSpace(Profile?.FirstName) || !string.IsNullOrWhiteSpace(Profile?.LastName)
                ? string.Join(" ", new[] { Profile?.FirstName, Profile?.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : !string.IsNullOrWhiteSpace(Customer?.FirstName) || !string.IsNullOrWhiteSpace(Customer?.LastName)
                    ? string.Join(" ", new[] { Customer?.FirstName, Customer?.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                    : string.IsNullOrWhiteSpace(CurrentCustomer.Email) ? "Customer" : CurrentCustomer.Email;
}