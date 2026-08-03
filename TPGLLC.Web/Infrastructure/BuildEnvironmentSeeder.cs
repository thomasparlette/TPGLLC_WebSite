using System.Text.Json;
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

        foreach (var roleName in new[] { "Customer", "Employee", "Administrator" })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

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
                CreatedUtc = DateTimeOffset.UtcNow
            };

            _db.Customers.Add(customer);
        }

        customer.FirstName = "Thomas";
        customer.LastName = "Parlette";
        customer.Email = email;
        customer.Phone = "(765) 346-3354";
        customer.AddressLine1 = "2203 Mcclennan Ct S";
        customer.AddressLine2 = null;
        customer.City = "Columbus";
        customer.State = "IN";
        customer.PostalCode = "47203";
        customer.Notes = "Build demo customer";

        var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
        if (profile is null)
        {
            profile = new CustomerProfile
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            _db.CustomerProfiles.Add(profile);
        }

        profile.FirstName = "Thomas";
        profile.LastName = "Parlette";
        profile.Phone = "(765) 346-3354";
        profile.Address1 = "2203 Mcclennan Ct S";
        profile.Address2 = null;
        profile.City = "Columbus";
        profile.State = "IN";
        profile.ZipCode = "47203";

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
            _db.CustomerVehicles.Add(new CustomerVehicle
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
            _db.AppointmentRequests.Add(new AppointmentRequest
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
                    Mileage = 52000,
                    Technician = "J. Miller",
                    Status = "Completed",
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
                    Mileage = 6050,
                    Technician = "A. Smith",
                    Status = "Completed",
                    Notes = "Demo history row",
                    CreatedUtc = DateTimeOffset.UtcNow
                }
            ]);
        }
        await SeedVehicleCatalogAsync();

        await _db.SaveChangesAsync();
    }

    private async Task SeedVehicleCatalogAsync()
    {
        var seedPath = Path.Combine(_environment.ContentRootPath, "Services", "Portal", "VehicleSeedData.json");
        if (!File.Exists(seedPath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath);
        var records = JsonSerializer.Deserialize<List<VehicleSeedRecord>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        foreach (var record in records)
        {
            var existing = await _db.VehicleCatalogEntries.FirstOrDefaultAsync(x =>
                x.ModelYear == record.ModelYear &&
                x.MakeId == record.MakeId &&
                x.ModelId == record.ModelId);

            if (existing is null)
            {
                _db.VehicleCatalogEntries.Add(new VehicleCatalogEntry
                {
                    ModelYear = record.ModelYear,
                    MakeId = record.MakeId,
                    ModelId = record.ModelId,
                    Make = record.Make,
                    Model = record.Model,
                    SyncedAtUtc = record.SyncedAtUtc
                });
            }
            else
            {
                existing.ModelYear = record.ModelYear;
                existing.MakeId = record.MakeId;
                existing.ModelId = record.ModelId;
                existing.Make = record.Make;
                existing.Model = record.Model;
                existing.SyncedAtUtc = record.SyncedAtUtc;
            }
        }
    }
}