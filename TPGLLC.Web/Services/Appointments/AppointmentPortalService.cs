using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Services.Vehicles;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentPortalService : IAppointmentPortalService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IVehicleCatalogService _vehicleCatalogService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentPortalService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IVehicleCatalogService vehicleCatalogService,
        UserManager<ApplicationUser> userManager)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _vehicleCatalogService = vehicleCatalogService;
        _userManager = userManager;
    }

    public async Task<AppointmentPageViewModel> GetAsync()
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to request appointments."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.ChangeTracker.Clear();

        var profile = await db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        var user = await _userManager.FindByIdAsync(current.UserId);

        var customerInfo = BuildCustomerInfo(
            user,
            profile,
            customer,
            current.Email);

        var requests = new List<AppointmentRequest>();

        if (!string.IsNullOrWhiteSpace(customerInfo.Email))
        {
            requests = await db.AppointmentRequests
                .AsNoTracking()
                .Where(x => x.Email == customerInfo.Email)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToListAsync();
        }

        return new AppointmentPageViewModel
        {
            Requests = requests,
            OpenRequests = requests.Where(x => !IsClosedStatus(x.Status)).ToList(),
            Years = await GetYearsAsync(),
            Form = BuildDefaultForm(customerInfo)
        };
    }

    public async Task<AppointmentPageViewModel> ResetAsync()
    {
        return await GetAsync();
    }

    public async Task<AppointmentPageViewModel> YearChangedAsync(AppointmentPageViewModel model)
    {
        model.Form.VehicleMake = string.Empty;
        model.Form.VehicleModel = string.Empty;
        model.Makes = [];
        model.Models = [];

        if (TryParseYear(model.Form.VehicleYear, out var year) && year.HasValue)
        {
            model.Makes = (await _vehicleCatalogService.GetMakesAsync(year.Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        return model;
    }

    public async Task<AppointmentPageViewModel> MakeChangedAsync(AppointmentPageViewModel model)
    {
        model.Form.VehicleModel = string.Empty;
        model.Models = [];

        if (TryParseYear(model.Form.VehicleYear, out var year) &&
            year.HasValue &&
            !string.IsNullOrWhiteSpace(model.Form.VehicleMake))
        {
            model.Models = (await _vehicleCatalogService.GetModelsAsync(year.Value, model.Form.VehicleMake.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        return model;
    }

    public async Task<AppointmentPageViewModel> SaveAsync(AppointmentPageViewModel model)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to request appointments.";
            return model;
        }

        if (!TryParseYear(model.Form.VehicleYear, out _))
        {
            model.ErrorMessage = "Vehicle year must be a valid year.";
            return model;
        }

        if (!TryParseMileage(model.Form.Mileage, out _))
        {
            model.ErrorMessage = "Mileage must be a valid whole number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var profile = await db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        var user = await _userManager.FindByIdAsync(current.UserId);

        var customerInfo = BuildCustomerInfo(user, profile, customer, current.Email);

        var request = new AppointmentRequest
        {
            Name = string.IsNullOrWhiteSpace(model.Form.Name)
                ? customerInfo.Name
                : model.Form.Name.Trim(),

            Email = string.IsNullOrWhiteSpace(model.Form.Email)
                ? customerInfo.Email
                : model.Form.Email.Trim(),

            Phone = string.IsNullOrWhiteSpace(model.Form.Phone)
                ? customerInfo.Phone
                : model.Form.Phone.Trim(),

            VehicleYear = string.IsNullOrWhiteSpace(model.Form.VehicleYear) ? null : model.Form.VehicleYear.Trim(),
            VehicleMake = string.IsNullOrWhiteSpace(model.Form.VehicleMake) ? null : model.Form.VehicleMake.Trim(),
            VehicleModel = string.IsNullOrWhiteSpace(model.Form.VehicleModel) ? null : model.Form.VehicleModel.Trim(),
            Vin = string.IsNullOrWhiteSpace(model.Form.Vin) ? null : model.Form.Vin.Trim(),
            Mileage = string.IsNullOrWhiteSpace(model.Form.Mileage) ? null : model.Form.Mileage.Trim(),
            PreferredDate = model.Form.PreferredDate.Trim(),
            PreferredTime = model.Form.PreferredTime.Trim(),
            ServiceNeeded = model.Form.ServiceNeeded.Trim(),
            Status = "Requested",
            Message = string.IsNullOrWhiteSpace(model.Form.Message) ? null : model.Form.Message.Trim(),
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };

        db.AppointmentRequests.Add(request);
        await db.SaveChangesAsync();

        var updatedModel = await GetAsync();
        updatedModel.SuccessMessage = "Appointment request submitted.";
        return updatedModel;
    }

    public async Task<AppointmentPageViewModel> RescheduleAsync(
        Guid requestId,
        AppointmentRescheduleFormModel form,
        CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to reschedule appointments."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(current.UserId);
        var currentEmail = FirstNotEmpty(user?.Email, current.Email);

        if (string.IsNullOrWhiteSpace(currentEmail))
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "Your account does not have an email address."
            };
        }

        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(
                x => x.RequestId == requestId && x.Email == currentEmail,
                cancellationToken);

        if (request is null)
        {
            var notFoundModel = await GetAsync();
            notFoundModel.ErrorMessage = "Appointment request was not found.";
            return notFoundModel;
        }

        if (IsClosedStatus(request.Status))
        {
            var closedModel = await GetAsync();
            closedModel.ErrorMessage = "Closed appointments cannot be rescheduled.";
            return closedModel;
        }

        request.PreferredDate = form.PreferredDate.Trim();
        request.PreferredTime = form.PreferredTime.Trim();
        request.ServiceNeeded = form.ServiceNeeded.Trim();
        request.Message = string.IsNullOrWhiteSpace(form.Message) ? null : form.Message.Trim();
        request.Status = "Requested";

        await db.SaveChangesAsync(cancellationToken);

        var result = await GetAsync();
        result.SuccessMessage = "Appointment request rescheduled.";
        return result;
    }

    public async Task<AppointmentPageViewModel> CancelAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to cancel appointments."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(current.UserId);
        var currentEmail = FirstNotEmpty(user?.Email, current.Email);

        if (string.IsNullOrWhiteSpace(currentEmail))
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "Your account does not have an email address."
            };
        }

        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(
                x => x.RequestId == requestId && x.Email == currentEmail,
                cancellationToken);

        if (request is null)
        {
            var notFoundModel = await GetAsync();
            notFoundModel.ErrorMessage = "Appointment request was not found.";
            return notFoundModel;
        }

        if (!IsClosedStatus(request.Status))
        {
            request.Status = "Cancelled";
            await db.SaveChangesAsync(cancellationToken);
        }

        var result = await GetAsync();
        result.SuccessMessage = "Appointment request cancelled.";
        return result;
    }

    private async Task<List<int>> GetYearsAsync()
    {
        var years = await _vehicleCatalogService.GetYearsAsync();
        var list = years.Distinct().OrderByDescending(x => x).ToList();

        if (list.Count == 0)
        {
            list = Enumerable.Range(1995, DateTime.UtcNow.Year - 1995 + 1)
                .Reverse()
                .ToList();
        }

        return list;
    }

    private static AppointmentRequestFormModel BuildDefaultForm(CustomerAppointmentInfo customer)
    {
        return new AppointmentRequestFormModel
        {
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone
        };
    }

    private static CustomerAppointmentInfo BuildCustomerInfo(
        ApplicationUser? user,
        CustomerProfile? profile,
        Customer? customer,
        string? authenticatedEmail)
    {
        var firstName = FirstNotEmpty(
            user?.FirstName,
            profile?.FirstName,
            customer?.FirstName);

        var lastName = FirstNotEmpty(
            user?.LastName,
            profile?.LastName,
            customer?.LastName);

        var name = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(name))
        {
            name = FirstNotEmpty(
                user?.DisplayName,
                authenticatedEmail,
                "Customer");
        }

        var email = FirstNotEmpty(
            user?.Email,
            customer?.Email,
            authenticatedEmail);

        var phone = FirstNotEmpty(
            user?.PhoneNumber,
            profile?.Phone,
            customer?.Phone);

        return new CustomerAppointmentInfo(name, email, phone);
    }

    private static string FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
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

    private static bool TryParseYear(string? value, out int? year)
    {
        year = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (int.TryParse(value.Trim(), out var parsed) && parsed is >= 1900 and <= 3000)
        {
            year = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseMileage(string? value, out int? mileage)
    {
        mileage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Replace(",", string.Empty, StringComparison.Ordinal).Trim();
        if (int.TryParse(normalized, out var parsed) && parsed >= 0)
        {
            mileage = parsed;
            return true;
        }

        return false;
    }

    private sealed record CustomerAppointmentInfo(
        string Name,
        string Email,
        string Phone);
}