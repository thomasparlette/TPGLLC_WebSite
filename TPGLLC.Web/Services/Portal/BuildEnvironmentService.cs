using TPGLLC.Data.Entities;
using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.Components.PortalShared.Vehicles;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class BuildEnvironmentService : IBuildEnvironmentService
{
    private static readonly Guid DemoCustomerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PrimaryVehicleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondaryVehicleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DemoAppointmentRequestId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly bool _isBuildEnvironment;

    public BuildEnvironmentService(IWebHostEnvironment environment)
    {
        _isBuildEnvironment = environment.IsEnvironment("Build");
    }

    public bool IsBuildEnvironment => _isBuildEnvironment;

    public DashboardViewModel CreateDashboard()
    {
        var vehicles = CreateVehicleList();
        var requests = CreateAppointmentList();
        var history = CreateHistoryList();
        var workOrders = CreateWorkOrders();

        var model = new DashboardViewModel
        {
            DisplayName = "Thomas Parlette",
            Vehicles = vehicles,
            Requests = requests,
            History = history,
            WorkOrders = workOrders
        };

        model.Activity = new List<ActivityItem>
        {
            new("🚗", "Vehicle added", "2019 Dodge Challenger", DateTime.Today.AddDays(-12).ToString("MMM d, yyyy")),
            new("📅", "Appointment request", "Oil change and inspection", DateTime.Today.AddDays(-1).ToString("MMM d, yyyy")),
            new("📝", "Work order quoted", "Front brakes and rotors", DateTime.Today.AddDays(-3).ToString("MMM d, yyyy")),
            new("🛠️", "Service completed", "2019 Dodge Challenger · Oil change", DateTime.Today.AddDays(-21).ToString("MMM d, yyyy"))
        };

        return model;
    }

    public VehiclePageViewModel CreateVehicles(Guid? editingVehicleId = null)
    {
        var vehicles = CreateVehicleList();
        var selected = editingVehicleId is null
            ? null
            : vehicles.FirstOrDefault(x => x.Id == editingVehicleId.Value);

        var model = new VehiclePageViewModel
        {
            Vehicles = vehicles,
            Years = GetYears(),
            EditingVehicleId = editingVehicleId,
            Form = selected is null
                ? new VehicleFormModel()
                : new VehicleFormModel
                {
                    ModelYear = selected.ModelYear?.ToString() ?? string.Empty,
                    Make = selected.Make ?? string.Empty,
                    Model = selected.Model ?? string.Empty,
                    Vin = selected.Vin,
                    Nickname = selected.Nickname,
                    LicensePlate = selected.LicensePlate,
                    Mileage = selected.Mileage?.ToString(),
                    IsPrimary = selected.IsPrimary
                }
        };

        if (selected is not null && selected.ModelYear is int year && !string.IsNullOrWhiteSpace(selected.Make))
        {
            model.Makes = GetMakesForYear(year);
            model.Models = GetModelsForYearAndMake(year, selected.Make);
        }

        return model;
    }

    public VehicleDetailsViewModel CreateVehicleDetails(Guid vehicleId)
    {
        var vehicle = CreateVehicleList().FirstOrDefault(x => x.Id == vehicleId);
        if (vehicle is null)
        {
            vehicle = CreateVehicleList().First();
        }

        var history = CreateHistoryList()
            .Where(x => x.CustomerVehicleId == vehicle.Id)
            .ToList();

        return new VehicleDetailsViewModel
        {
            Vehicle = vehicle,
            ServiceHistory = history
        };
    }

    public AppointmentPageViewModel CreateAppointments()
    {
        var requests = CreateAppointmentList();
        var year = 2019;
        var make = "Dodge";

        return new AppointmentPageViewModel
        {
            Requests = requests,
            OpenRequests = requests.Where(x => !IsClosedStatus(x.Status)).ToList(),
            Years = GetYears(),
            Makes = GetMakesForYear(year),
            Models = GetModelsForYearAndMake(year, make),
            Form = new AppointmentRequestFormModel
            {
                Name = "Thomas Parlette",
                Email = "thomasparlette@gmail.com",
                Phone = "765-346-3354",
                VehicleYear = year.ToString(),
                VehicleMake = make,
                VehicleModel = "Challenger",
                Mileage = "52800",
                ServiceNeeded = "Oil change and inspection",
                PreferredDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"),
                PreferredTime = "09:00",
                Message = "Please check brakes and tire wear.",
                Vin = "2C3CDZC94KH123456"
            }
        };
    }

    private static List<CustomerVehicle> CreateVehicleList()
    {
        return
        [
            new CustomerVehicle
            {
                Id = PrimaryVehicleId,
                CustomerId = DemoCustomerId,
                ModelYear = 2019,
                Make = "Dodge",
                Model = "Challenger",
                Vin = "2C3CDZC94KH123456",
                Nickname = "Weekend driver",
                LicensePlate = "DEMO-1",
                Mileage = 52_800,
                IsPrimary = true,
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-12)
            },
            new CustomerVehicle
            {
                Id = SecondaryVehicleId,
                CustomerId = DemoCustomerId,
                ModelYear = 2015,
                Make = "Honda",
                Model = "Grom",
                Vin = "JH2JC75K5FK123456",
                Nickname = "Commuter bike",
                LicensePlate = "DEMO-2",
                Mileage = 6_200,
                IsPrimary = false,
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-6)
            }
        ];
    }

    private static List<ServiceHistoryEntry> CreateHistoryList()
    {
        return
        [
            new ServiceHistoryEntry
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                CustomerId = DemoCustomerId,
                CustomerVehicleId = PrimaryVehicleId,
                VehicleName = "2019 Dodge Challenger",
                ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-21)),
                Service = "Oil change",
                WorkOrderNumber = "WO-10482",
                Complaint = "Routine maintenance request",
                Diagnosis = "Oil change and inspection completed without issues.",
                Mileage = 52_000,
                Technician = "Demo Tech",
                Status = "Completed",
                ApprovalStatus = "Approved",
                EstimateAmount = 89.95m,
                InvoiceNumber = "INV-10482",
                InvoiceAmount = 89.95m,
                Notes = "Oil and filter replaced. Tires rotated.",
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-21)
            },
            new ServiceHistoryEntry
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                CustomerId = DemoCustomerId,
                CustomerVehicleId = SecondaryVehicleId,
                VehicleName = "2015 Honda Grom",
                ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-45)),
                Service = "Chain service",
                WorkOrderNumber = "WO-10491",
                Complaint = "Chain adjustment and lubrication needed",
                Diagnosis = "Chain cleaned, adjusted, and lubricated.",
                Mileage = 6_050,
                Technician = "Demo Tech",
                Status = "Completed",
                ApprovalStatus = "Approved",
                EstimateAmount = 164.20m,
                InvoiceNumber = "INV-10491",
                InvoiceAmount = 164.20m,
                Notes = "Chain cleaned, adjusted, and lubricated.",
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-45)
            }
        ];
    }

    private static List<AppointmentRequest> CreateAppointmentList()
    {
        return
        [
            new AppointmentRequest
            {
                RequestId = DemoAppointmentRequestId,
                Name = "Thomas Parlette",
                Email = "thomasparlette@gmail.com",
                Phone = "765-346-3354",
                VehicleYear = "2019",
                VehicleMake = "Dodge",
                VehicleModel = "Challenger",
                Vin = "2C3CDZC94KH123456",
                Mileage = "52800",
                PreferredDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"),
                PreferredTime = "09:00",
                ServiceNeeded = "Oil change and inspection",
                Message = "Please check brakes and tire wear.",
                Status = "Requested",
                SubmittedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
            }
        ];
    }

    public WorkOrderPageViewModel CreateWorkOrders()
    {
        return new WorkOrderPageViewModel
        {
            WorkOrders = CreateHistoryList(),
            CanEdit = true
        };
    }

    private static List<int> GetYears()
    {
        return Enumerable.Range(1995, DateTime.UtcNow.Year - 1995 + 1)
            .Reverse()
            .ToList();
    }

    private static List<string> GetMakesForYear(int year)
    {
        var makes = year switch
        {
            2019 => new[] { "Dodge", "Ford", "Honda", "Toyota" },
            2015 => new[] { "Honda", "Kawasaki", "Yamaha" },
            _ => new[] { "Dodge", "Ford", "Honda", "Toyota" }
        };

        return makes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> GetModelsForYearAndMake(int year, string make)
    {
        var key = (year, make.Trim().ToUpperInvariant());

        return key switch
        {
            (2019, "DODGE") => new List<string> { "Challenger", "Charger", "Durango" },
            (2019, "FORD") => new List<string> { "F-150", "Escape", "Explorer" },
            (2019, "HONDA") => new List<string> { "Accord", "Civic", "CR-V" },
            (2015, "HONDA") => new List<string> { "Grom", "CR-V", "Civic" },
            (2015, "KAWASAKI") => new List<string> { "Ninja 300", "Versys", "Vulcan S" },
            (2015, "YAMAHA") => new List<string> { "R3", "Bolt", "YZF-R3" },
            _ => []
        };
    }

    private static bool IsClosedStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Declined", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Closed", StringComparison.OrdinalIgnoreCase);
    }
}

