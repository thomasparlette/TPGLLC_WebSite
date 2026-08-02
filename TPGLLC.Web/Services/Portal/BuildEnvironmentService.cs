using TPGLLC.Data.Entities;
using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.Components.PortalShared.Vehicles;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class BuildEnvironmentService : IBuildEnvironmentService
{
    private readonly bool _isBuildEnvironment;

    public BuildEnvironmentService(IWebHostEnvironment environment)
    {
        _isBuildEnvironment = environment.IsEnvironment("Build");
    }

    public bool IsBuildEnvironment => _isBuildEnvironment;

    public DashboardViewModel CreateDashboard()
    {
        var vehicles = new List<CustomerVehicle>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ModelYear = 2016,
                Make = "Acura",
                Model = "MDX",
                Nickname = "Family SUV",
                LicensePlate = "DEMO-1",
                Mileage = 84_521,
                IsPrimary = true,
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-12)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ModelYear = 2021,
                Make = "Ford",
                Model = "F-150",
                Nickname = "Truck",
                LicensePlate = "DEMO-2",
                Mileage = 31_240,
                IsPrimary = false,
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-6)
            }
        };

        var requests = new List<AppointmentRequest>
        {
            new()
            {
                Name = "Thomas Parlette",
                Email = "thomasparlette@gmail.com",
                Phone = "555-000-1000",
                VehicleYear = "2016",
                VehicleMake = "Acura",
                VehicleModel = "MDX",
                ServiceNeeded = "Oil change and tire rotation",
                PreferredDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"),
                PreferredTime = "09:00",
                Status = "Requested",
                SubmittedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        var history = new List<ServiceHistoryEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                VehicleName = "2016 Acura MDX",
                ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-18)),
                Service = "Brake inspection",
                Mileage = 84_101,
                Technician = "Demo Tech",
                Status = "Completed",
                Notes = "Pads and rotors inspected. No issues found.",
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-18)
            }
        };

        var model = new DashboardViewModel
        {
            DisplayName = "Customer",
            Vehicles = vehicles,
            Requests = requests,
            History = history
        };

        model.Activity = new List<ActivityItem>
        {
            new("🚗", "Vehicle added", "2016 Acura MDX", DateTime.Today.AddDays(-12).ToString("MMM d, yyyy")),
            new("📅", "Appointment request", "Oil change and tire rotation", DateTime.Today.AddDays(-1).ToString("MMM d, yyyy")),
            new("🛠️", "Service completed", "2016 Acura MDX · Brake inspection", DateTime.Today.AddDays(-18).ToString("MMM d, yyyy"))
        };

        return model;
    }

    public VehiclePageViewModel CreateVehicles()
    {
        return new VehiclePageViewModel
        {
            Vehicles =
            [
                new CustomerVehicle
                {
                    Id = Guid.NewGuid(),
                    ModelYear = 2016,
                    Make = "Acura",
                    Model = "MDX",
                    Nickname = "Family SUV",
                    LicensePlate = "DEMO-1",
                    Mileage = 84_521,
                    IsPrimary = true,
                    CreatedUtc = DateTimeOffset.UtcNow.AddDays(-12)
                },
                new CustomerVehicle
                {
                    Id = Guid.NewGuid(),
                    ModelYear = 2021,
                    Make = "Ford",
                    Model = "F-150",
                    Nickname = "Truck",
                    LicensePlate = "DEMO-2",
                    Mileage = 31_240,
                    IsPrimary = false,
                    CreatedUtc = DateTimeOffset.UtcNow.AddDays(-6)
                }
            ],
            Years = GetYears(),
            Form = new VehicleFormModel
            {
                IsPrimary = false
            }
        };
    }

    public AppointmentPageViewModel CreateAppointments()
    {
        return new AppointmentPageViewModel
        {
            Requests =
            [
                new AppointmentRequest
                {
                    Name = "Thomas Parlette",
                    Email = "thomasparlette@gmail.com",
                    Phone = "555-000-1000",
                    VehicleYear = "2016",
                    VehicleMake = "Acura",
                    VehicleModel = "MDX",
                    ServiceNeeded = "Oil change and tire rotation",
                    PreferredDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"),
                    PreferredTime = "09:00",
                    Status = "Requested",
                    SubmittedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
                }
            ],
            OpenRequests =
            [
                new AppointmentRequest
                {
                    Name = "Thomas Parlette",
                    Email = "thomasparlette@gmail.com",
                    Phone = "555-000-1000",
                    VehicleYear = "2016",
                    VehicleMake = "Acura",
                    VehicleModel = "MDX",
                    ServiceNeeded = "Oil change and tire rotation",
                    PreferredDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"),
                    PreferredTime = "09:00",
                    Status = "Requested",
                    SubmittedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
                }
            ],
            Years = GetYears(),
            Form = new AppointmentRequestFormModel
            {
                Name = "Thomas Parlette",
                Email = "thomasparlette@gmail.com",
                Phone = "555-000-1000"
            }
        };
    }

    private static List<int> GetYears()
    {
        return Enumerable.Range(1995, DateTime.UtcNow.Year - 1995 + 1)
            .Reverse()
            .ToList();
    }
}