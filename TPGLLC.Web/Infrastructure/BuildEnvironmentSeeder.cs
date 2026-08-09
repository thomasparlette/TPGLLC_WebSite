using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Infrastructure;

public sealed class BuildEnvironmentSeeder
{
    private readonly TPGLLCDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IWebHostEnvironment _environment;

    public BuildEnvironmentSeeder(
        TPGLLCDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
    }

    public async Task SeedAsync()
    {
        await _db.Database.EnsureCreatedAsync();

        await EnsureRolesAsync();
        await SeedVehicleCatalogAsync();

        var email = "thomasparlette@gmail.com";
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Thomas Parlette",
                FirstName = "Thomas",
                LastName = "Parlette",
                IsActive = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, "BuildDemo123!");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to create build demo user: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "Customer");
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
        if (customer is null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                FirstName = "Thomas",
                LastName = "Parlette",
                Email = email,
                Phone = "(765) 346-3354",
                AddressLine1 = "2203 Mcclennan Ct S",
                AddressLine2 = null,
                City = "Columbus",
                State = "IN",
                PostalCode = "47203",
                Notes = "Build demo customer",
                CreatedUtc = DateTimeOffset.UtcNow
            };
            _db.Customers.Add(customer);
        }

        var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
        if (profile is null)
        {
            profile = new CustomerProfile
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                FirstName = "Thomas",
                LastName = "Parlette",
                Phone = "(765) 346-3354",
                Address1 = "2203 Mcclennan Ct S",
                Address2 = null,
                City = "Columbus",
                State = "IN",
                ZipCode = "47203",
                Country = "USA",
                PreferredContactMethod = "Email",
                ReceiveEmail = true,
                ReceiveSms = false,
                CreatedUtc = DateTimeOffset.UtcNow
            };
            _db.CustomerProfiles.Add(profile);
        }

        var primaryVehicle = await _db.CustomerVehicles
            .FirstOrDefaultAsync(x => x.CustomerId == customer.Id && x.IsPrimary);

        if (primaryVehicle is null)
        {
            primaryVehicle = new CustomerVehicle
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                ModelYear = 2019,
                Make = "Dodge",
                Model = "Challenger",
                Vin = "2C3CDZC94KH123456",
                Nickname = "Weekend driver",
                LicensePlate = "DEMO-1",
                Mileage = 52800,
                IsPrimary = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };
            _db.CustomerVehicles.Add(primaryVehicle);
        }

        if (!await _db.CustomerVehicles.AnyAsync(x => x.CustomerId == customer.Id && !x.IsPrimary))
        {
            _db.CustomerVehicles.Add(
                new CustomerVehicle
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    ModelYear = 2015,
                    Make = "Honda",
                    Model = "Grom",
                    Vin = "JH2JC75K5FK123456",
                    Nickname = "Commuter bike",
                    LicensePlate = "DEMO-2",
                    Mileage = 6200,
                    IsPrimary = false,
                    CreatedUtc = DateTimeOffset.UtcNow
                });
        }

        if (!await _db.AppointmentRequests.AnyAsync(x => x.Email == email))
        {
            _db.AppointmentRequests.Add(
                new AppointmentRequest
                {
                    RequestId = Guid.NewGuid(),
                    Name = "Thomas Parlette",
                    Phone = "(765) 346-3354",
                    Email = email,
                    VehicleYear = "2019",
                    VehicleMake = "Dodge",
                    VehicleModel = "Challenger",
                    Vin = "2C3CDZC94KH123456",
                    Mileage = "52800",
                    PreferredDate = DateTime.Today.AddDays(4).ToString("yyyy-MM-dd"),
                    PreferredTime = "09:00",
                    ServiceNeeded = "Oil change and inspection",
                    Message = "Please check brakes and tire wear.",
                    Status = "Requested",
                    SubmittedAtUtc = DateTimeOffset.UtcNow
                });
        }

        if (!await _db.ServiceHistoryEntries.AnyAsync(x => x.CustomerId == customer.Id))
        {
            _db.ServiceHistoryEntries.AddRange(
            [
                new ServiceHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CustomerVehicleId = primaryVehicle.Id,
                    VehicleName = "2019 Dodge Challenger",
                    ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-21)),
                    Service = "Oil change",
                    WorkOrderNumber = "WO-10482",
                    Complaint = "Routine maintenance request",
                    Diagnosis = "Oil change and inspection completed without issues.",
                    Mileage = 52000,
                    Technician = "J. Miller",
                    Status = "Completed",
                    ApprovalStatus = "Approved",
                    EstimateAmount = 89.95m,
                    InvoiceNumber = "INV-10482",
                    InvoiceAmount = 89.95m,
                    Notes = "Demo history row",
                    CreatedUtc = DateTimeOffset.UtcNow
                },
                new ServiceHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CustomerVehicleId = null,
                    VehicleName = "2015 Honda Grom",
                    ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-45)),
                    Service = "Chain service",
                    WorkOrderNumber = "WO-10491",
                    Complaint = "Chain adjustment and lubrication needed",
                    Diagnosis = "Chain cleaned, adjusted, and lubricated.",
                    Mileage = 6050,
                    Technician = "A. Smith",
                    Status = "Completed",
                    ApprovalStatus = "Approved",
                    EstimateAmount = 164.20m,
                    InvoiceNumber = "INV-10491",
                    InvoiceAmount = 164.20m,
                    Notes = "Demo history row",
                    CreatedUtc = DateTimeOffset.UtcNow
                }
            ]);
        }


        if (!await _db.ServiceHistoryEntries.AnyAsync(x => x.CustomerId == customer.Id && x.WorkOrderNumber == "WO-10500"))
        {
            _db.ServiceHistoryEntries.Add(
                new ServiceHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CustomerVehicleId = primaryVehicle.Id,
                    VehicleName = "2019 Dodge Challenger",
                    ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                    Service = "Front brakes and rotors",
                    WorkOrderNumber = "WO-10500",
                    Complaint = "Brake pulsation and noise under light braking.",
                    Diagnosis = "Front rotors are warped and pads are near minimum thickness.",
                    Mileage = 52950,
                    Technician = "Demo Tech",
                    Status = "Quoted",
                    ApprovalStatus = "Pending",
                    EstimateAmount = 684.27m,
                    Notes = "Customer advised of brake condition and quoted replacement.",
                    CreatedUtc = DateTimeOffset.UtcNow
                });
        }
        await _db.SaveChangesAsync();
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var roleName in new[] { "Customer", "Employee", "Administrator" })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }
    }

    private async Task SeedVehicleCatalogAsync()
    {
        if (await _db.VehicleCatalogEntries.AnyAsync())
        {
            return;
        }

        var seedPath = GetVehicleSeedDataPath();
        if (seedPath is null || !File.Exists(seedPath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath);
        var rows = JsonSerializer.Deserialize<List<VehicleSeedRow>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (rows is null || rows.Count == 0)
        {
            return;
        }

        var entries = rows
            .Where(row => row.ModelYear > 0)
            .Select(row => new VehicleCatalogEntry
            {
                Id = row.Id,
                ModelYear = row.ModelYear,
                MakeId = row.MakeId,
                ModelId = row.ModelId,
                Make = Normalize(row.Make),
                Model = Normalize(row.Model),
                SyncedAtUtc = row.SyncedAtUtc
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Make) && !string.IsNullOrWhiteSpace(entry.Model))
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        await _db.VehicleCatalogEntries.AddRangeAsync(entries);
        await _db.SaveChangesAsync();
    }

    private string? GetVehicleSeedDataPath()
    {
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "Services", "Portal", "VehicleSeedData.json"),
            Path.Combine(AppContext.BaseDirectory, "Services", "Portal", "VehicleSeedData.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Services", "Portal", "VehicleSeedData.json")
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static string ResolveVehicleType(string? make, string? model)
    {
        var text = string.Join(' ', new[] { make, model }
            .Where(x => !string.IsNullOrWhiteSpace(x)))
            .Trim();

        var motorcycleHints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Can-Am",
            "CFMOTO",
            "Ducati",
            "Gas Gas",
            "Harley-Davidson",
            "Husqvarna",
            "Indian",
            "KTM",
            "Kawasaki",
            "Polaris",
            "Suzuki",
            "Triumph",
            "Vespa",
            "Yamaha",
            "Honda"
        };

        if (!string.IsNullOrWhiteSpace(make) && motorcycleHints.Contains(make.Trim()))
        {
            return "Motorcycle";
        }

        if (text.Contains("Moto", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ATV", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("SxS", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("UTV", StringComparison.OrdinalIgnoreCase))
        {
            return "Motorcycle";
        }

        return "Automotive";
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private sealed class VehicleSeedRow
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("ModelYear")]
        public int ModelYear { get; set; }

        [JsonPropertyName("MakeId")]
        public int MakeId { get; set; }

        [JsonPropertyName("ModelId")]
        public int ModelId { get; set; }

        [JsonPropertyName("Make")]
        public string Make { get; set; } = string.Empty;

        [JsonPropertyName("Model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("SyncedAtUtc")]
        public DateTimeOffset SyncedAtUtc { get; set; }
    }
}
