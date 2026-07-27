using TPGLLC.Application.Contracts.V1.Portal;

namespace TPGLLC.Web.Features.Portal;

public sealed class CustomerPortalStore
{
    private readonly List<CustomerVehicleDto> _vehicles = new();
    private readonly List<CustomerAppointmentDto> _appointments = new();
    private readonly List<ServiceHistoryEntryDto> _history = new();
    private readonly List<InvoiceEntryDto> _invoices = new();

    public CustomerProfileDto Profile { get; private set; } = new(
        DisplayName: "Thomas Parlette",
        Email: "tomparlette@tomparlettegarage.org",
        Phone: "(765) 346-3354",
        AddressLine1: "123 Main St",
        AddressLine2: null,
        City: "Seymour",
        State: "IN",
        PostalCode: "47274");

    public IReadOnlyList<CustomerVehicleDto> Vehicles => _vehicles;
    public IReadOnlyList<CustomerAppointmentDto> Appointments => _appointments;
    public IReadOnlyList<ServiceHistoryEntryDto> History => _history;
    public IReadOnlyList<InvoiceEntryDto> Invoices => _invoices;

    public CustomerPortalStore()
    {
        _vehicles.AddRange([
            new CustomerVehicleDto(
                Id: Guid.NewGuid(),
                DisplayName: "2019 Dodge Challenger",
                VehicleType: "Automotive",
                Year: "2019",
                Make: "Dodge",
                Model: "Challenger",
                Trim: "6.4L HEMI",
                Vin: "2C3CDZC94KH123456",
                Mileage: "52,800",
                IsPrimary: true,
                Notes: "Weekend driver"),

            new CustomerVehicleDto(
                Id: Guid.NewGuid(),
                DisplayName: "2015 Honda Grom",
                VehicleType: "Motorcycle",
                Year: "2015",
                Make: "Honda",
                Model: "Grom",
                Trim: "186cc",
                Vin: "JH2JC75K5FK123456",
                Mileage: "6,200",
                IsPrimary: false,
                Notes: "Commuter bike")
        ]);

        _appointments.AddRange([
            new CustomerAppointmentDto(
                Id: Guid.NewGuid(),
                VehicleName: "2019 Dodge Challenger",
                Service: "Oil change and inspection",
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
                Time: new TimeOnly(9, 0),
                Status: "Confirmed",
                Notes: "Please check brakes and tire wear.")
        ]);

        _history.AddRange([
            new ServiceHistoryEntryDto(
                Id: Guid.NewGuid(),
                VehicleName: "2019 Dodge Challenger",
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-21)),
                Service: "Oil change",
                Mileage: 52_000,
                Technician: "J. Miller",
                Status: "Completed"),

            new ServiceHistoryEntryDto(
                Id: Guid.NewGuid(),
                VehicleName: "2015 Honda Grom",
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-45)),
                Service: "Chain service",
                Mileage: 6_050,
                Technician: "A. Smith",
                Status: "Completed"),

            new ServiceHistoryEntryDto(
                Id: Guid.NewGuid(),
                VehicleName: "2019 Dodge Challenger",
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-68)),
                Service: "Brake inspection",
                Mileage: 51_400,
                Technician: "K. Brown",
                Status: "Completed")
        ]);

        _invoices.AddRange([
            new InvoiceEntryDto(
                Id: Guid.NewGuid(),
                InvoiceNumber: "INV-10482",
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-21)),
                VehicleName: "2019 Dodge Challenger",
                Total: 89.95m,
                Paid: true,
                Status: "Paid"),

            new InvoiceEntryDto(
                Id: Guid.NewGuid(),
                InvoiceNumber: "INV-10491",
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-45)),
                VehicleName: "2015 Honda Grom",
                Total: 164.20m,
                Paid: false,
                Status: "Outstanding")
        ]);
    }

    public void UpdateProfile(CustomerProfileDto profile) => Profile = profile;

    public void AddVehicle(CustomerVehicleDto vehicle) => _vehicles.Insert(0, vehicle);

    public void AddAppointment(CustomerAppointmentDto appointment) => _appointments.Insert(0, appointment);

    public void AddHistory(ServiceHistoryEntryDto history) => _history.Insert(0, history);

    public void AddInvoice(InvoiceEntryDto invoice) => _invoices.Insert(0, invoice);
}