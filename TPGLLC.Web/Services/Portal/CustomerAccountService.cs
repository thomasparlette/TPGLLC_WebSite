using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IBuildEnvironmentService _buildEnvironmentService;

    public CustomerAccountService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IBuildEnvironmentService buildEnvironmentService)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _buildEnvironmentService = buildEnvironmentService;
    }

    public async Task<CustomerAccountViewModel> GetAsync()
    {
        if (_buildEnvironmentService.IsBuildEnvironment)
        {
            return CreateDemoModel();
        }

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new CustomerAccountViewModel
            {
                ErrorMessage = "You must be signed in to view your account."
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

        return new CustomerAccountViewModel
        {
            FirstName = profile?.FirstName ?? customer?.FirstName ?? string.Empty,
            LastName = profile?.LastName ?? customer?.LastName ?? string.Empty,
            Company = profile?.Company ?? string.Empty,
            Phone = profile?.Phone ?? customer?.Phone ?? string.Empty,
            AddressLine1 = profile?.Address1 ?? customer?.AddressLine1 ?? string.Empty,
            AddressLine2 = profile?.Address2 ?? customer?.AddressLine2 ?? string.Empty,
            City = profile?.City ?? customer?.City ?? string.Empty,
            State = profile?.State ?? customer?.State ?? string.Empty,
            ZipCode = profile?.ZipCode ?? customer?.PostalCode ?? string.Empty,
            Email = current.Email ?? customer?.Email ?? string.Empty
        };
    }

    public async Task<CustomerAccountViewModel> SaveAsync(CustomerAccountViewModel model)
    {
        if (_buildEnvironmentService.IsBuildEnvironment)
        {
            model.SuccessMessage = "Build environment does not save account changes.";
            return model;
        }

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to update your account.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var profile = await db.CustomerProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        if (profile is null)
        {
            profile = new CustomerProfile
            {
                ApplicationUserId = current.UserId
            };

            db.CustomerProfiles.Add(profile);
        }

        profile.FirstName = model.FirstName.Trim();
        profile.LastName = model.LastName.Trim();
        profile.Company = string.IsNullOrWhiteSpace(model.Company) ? null : model.Company.Trim();
        profile.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        profile.Address1 = string.IsNullOrWhiteSpace(model.AddressLine1) ? null : model.AddressLine1.Trim();
        profile.Address2 = string.IsNullOrWhiteSpace(model.AddressLine2) ? null : model.AddressLine2.Trim();
        profile.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        profile.State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim();
        profile.ZipCode = string.IsNullOrWhiteSpace(model.ZipCode) ? null : model.ZipCode.Trim();

        var customer = await db.Customers
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        if (customer is null)
        {
            customer = new Customer
            {
                ApplicationUserId = current.UserId
            };

            db.Customers.Add(customer);
        }

        customer.FirstName = profile.FirstName;
        customer.LastName = profile.LastName;
        customer.Phone = profile.Phone;
        customer.AddressLine1 = profile.Address1;
        customer.AddressLine2 = profile.Address2;
        customer.City = profile.City;
        customer.State = profile.State;
        customer.PostalCode = profile.ZipCode;
        customer.Email = string.IsNullOrWhiteSpace(current.Email) ? customer.Email : current.Email.Trim();

        await db.SaveChangesAsync();

        var refreshed = await GetAsync();
        refreshed.SuccessMessage = "Account details updated.";
        return refreshed;
    }

    private static CustomerAccountViewModel CreateDemoModel()
    {
        return new CustomerAccountViewModel
        {
            FirstName = "Thomas",
            LastName = "Parlette",
            Company = "TPG LLC",
            Phone = "(765) 346-3354",
            AddressLine1 = "123 Main Street",
            AddressLine2 = string.Empty,
            City = "Indianapolis",
            State = "IN",
            ZipCode = "46201",
            Email = "thomasparlette@gmail.com"
        };
    }
}