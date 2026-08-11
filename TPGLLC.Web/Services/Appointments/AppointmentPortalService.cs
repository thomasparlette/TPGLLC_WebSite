using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Services.Vehicles;
using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentPortalService : IAppointmentPortalService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly ICustomerProfileService _customerProfileService;
    private readonly IVehicleCatalogService _vehicleCatalogService;

    public AppointmentPortalService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        ICustomerProfileService customerProfileService,
        IVehicleCatalogService vehicleCatalogService)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _customerProfileService = customerProfileService;
        _vehicleCatalogService = vehicleCatalogService;
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

        List<AppointmentRequest> requests = [];

        if (!string.IsNullOrWhiteSpace(current.Email))
        {
            requests = await db.AppointmentRequests
                .AsNoTracking()
                .Where(x => x.Email == current.Email)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToListAsync();
        }

        return new AppointmentPageViewModel
        {
            Requests = requests,
            OpenRequests = requests.Where(x => !IsClosedStatus(x.Status)).ToList(),
            Years = await GetYearsAsync(),
            Form = BuildDefaultForm(profile, current.Email)
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

        var request = new AppointmentRequest
        {
            Name = string.IsNullOrWhiteSpace(model.Form.Name)
                ? BuildDisplayName(profile, current.Email)
                : model.Form.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Form.Email)
                ? (current.Email ?? string.Empty).Trim()
                : model.Form.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Form.Phone)
                ? (profile?.Phone ?? string.Empty).Trim()
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
        ArgumentNullException.ThrowIfNull(form);

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to update appointments."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId && x.Email == current.Email, cancellationToken);

        if (request is null)
        {
            var model = await GetAsync();
            model.ErrorMessage = "Appointment request was not found.";
            return model;
        }

        if (HasPendingProposal(request))
        {
            var model = await GetAsync();
            model.ErrorMessage = "Please approve or decline the proposed appointment time before requesting another change.";
            return model;
        }

        request.PreferredDate = form.PreferredDate.Trim();
        request.PreferredTime = form.PreferredTime.Trim();
        request.ServiceNeeded = form.ServiceNeeded.Trim();
        request.Message = string.IsNullOrWhiteSpace(form.Message) ? null : form.Message.Trim();
        request.Status = "Requested";
        request.SubmittedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var updatedModel = await GetAsync();
        updatedModel.SuccessMessage = "Appointment request updated.";
        return updatedModel;
    }

    public async Task<AppointmentPageViewModel> ApproveProposedRescheduleAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to approve appointment changes."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(
                x => x.RequestId == requestId && x.Email == current.Email,
                cancellationToken);

        if (request is null)
        {
            var model = await GetAsync();
            model.ErrorMessage = "Appointment request was not found.";
            return model;
        }

        if (!HasPendingProposal(request))
        {
            var model = await GetAsync();
            model.ErrorMessage = "There is no pending appointment time change for this request.";
            return model;
        }

        request.PreferredDate = request.ProposedDate!.Trim();
        request.PreferredTime = request.ProposedTime!.Trim();
        request.ProposedDate = null;
        request.ProposedTime = null;
        request.AdvisorMessage = null;
        request.Status = "Confirmed";

        await db.SaveChangesAsync(cancellationToken);

        var updatedModel = await GetAsync();
        updatedModel.SuccessMessage = "The new appointment time has been approved.";
        return updatedModel;
    }

    public async Task<AppointmentPageViewModel> DeclineProposedRescheduleAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to respond to appointment changes."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(
                x => x.RequestId == requestId && x.Email == current.Email,
                cancellationToken);

        if (request is null)
        {
            var model = await GetAsync();
            model.ErrorMessage = "Appointment request was not found.";
            return model;
        }

        if (!HasPendingProposal(request))
        {
            var model = await GetAsync();
            model.ErrorMessage = "There is no pending appointment time change for this request.";
            return model;
        }

        request.ProposedDate = null;
        request.ProposedTime = null;
        request.AdvisorMessage = null;
        request.Status = "Requested";

        await db.SaveChangesAsync(cancellationToken);

        var updatedModel = await GetAsync();
        updatedModel.SuccessMessage = "The proposed appointment time was declined. Your original request remains active.";
        return updatedModel;
    }

    public async Task<AppointmentPageViewModel> CancelAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new AppointmentPageViewModel
            {
                ErrorMessage = "You must be signed in to update appointments."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId && x.Email == current.Email, cancellationToken);

        if (request is null)
        {
            var model = await GetAsync();
            model.ErrorMessage = "Appointment request was not found.";
            return model;
        }

        request.Status = "Cancelled";
        await db.SaveChangesAsync(cancellationToken);

        var updatedModel = await GetAsync();
        updatedModel.SuccessMessage = "Appointment request cancelled.";
        return updatedModel;
    }

    private static bool HasPendingProposal(AppointmentRequest request)
    {
        return request.Status.Equals("RescheduleProposed", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.ProposedDate)
            && !string.IsNullOrWhiteSpace(request.ProposedTime);
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

    private static AppointmentRequestFormModel BuildDefaultForm(CustomerProfile? profile, string? email)
    {
        return new AppointmentRequestFormModel
        {
            Name = BuildDisplayName(profile, email),
            Email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(profile?.Phone) ? string.Empty : profile.Phone.Trim()
        };
    }

    private static string BuildDisplayName(CustomerProfile? profile, string? email)
    {
        var name = $"{profile?.FirstName} {profile?.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return string.IsNullOrWhiteSpace(email) ? "Customer" : email.Trim();
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
}
