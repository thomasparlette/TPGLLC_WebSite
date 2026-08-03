using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Web.Services.Customers;

namespace TPGLLC.Web.Services.Portal;

public sealed class VehiclePhotoService : IVehiclePhotoService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IBuildEnvironmentService _buildEnvironmentService;

    public VehiclePhotoService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        IWebHostEnvironment environment,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IBuildEnvironmentService buildEnvironmentService)
    {
        _dbFactory = dbFactory;
        _environment = environment;
        _currentCustomerAccessor = currentCustomerAccessor;
        _buildEnvironmentService = buildEnvironmentService;
    }

    public async Task UploadAsync(Guid vehicleId, IBrowserFile file)
    {

        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        var extension = Path.GetExtension(file.Name);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG, PNG, and WEBP images are allowed.");
        }

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            throw new InvalidOperationException("You must be signed in to upload a vehicle photo.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var vehicle = await db.CustomerVehicles
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.Customer.ApplicationUserId == current.UserId);

        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle not found.");
        }

        var webRoot = _environment.WebRootPath
            ?? throw new InvalidOperationException("Web root path is not configured.");

        var uploadFolder = Path.Combine(webRoot, "uploads", "vehicles");
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{vehicleId:N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(uploadFolder, fileName);

        await using (var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024))
        await using (var target = File.Create(absolutePath))
        {
            await stream.CopyToAsync(target);
        }

        vehicle.PhotoPath = $"/uploads/vehicles/{fileName}";
        vehicle.PhotoUpdatedUtc = DateTimeOffset.UtcNow;
        vehicle.UpdatedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid vehicleId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            throw new InvalidOperationException("You must be signed in to remove a vehicle photo.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var vehicle = await db.CustomerVehicles
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.Customer.ApplicationUserId == current.UserId);

        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle not found.");
        }

        if (!string.IsNullOrWhiteSpace(vehicle.PhotoPath))
        {
            var webRoot = _environment.WebRootPath;
            if (!string.IsNullOrWhiteSpace(webRoot))
            {
                var fileName = Path.GetFileName(vehicle.PhotoPath);
                var absolutePath = Path.Combine(webRoot, "uploads", "vehicles", fileName);

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
        }

        vehicle.PhotoPath = null;
        vehicle.PhotoUpdatedUtc = null;
        vehicle.UpdatedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
    }
}