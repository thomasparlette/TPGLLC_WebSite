using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerAccountService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<CustomerAccountViewModel> GetAsync()
    {
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

        var user = await _userManager.FindByIdAsync(current.UserId);

        return new CustomerAccountViewModel
        {
            FirstName = user?.FirstName ?? profile?.FirstName ?? customer?.FirstName ?? string.Empty,
            LastName = user?.LastName ?? profile?.LastName ?? customer?.LastName ?? string.Empty,
            Phone = profile?.Phone ?? customer?.Phone ?? string.Empty,
            AddressLine1 = profile?.Address1 ?? customer?.AddressLine1 ?? string.Empty,
            AddressLine2 = profile?.Address2 ?? customer?.AddressLine2 ?? string.Empty,
            City = profile?.City ?? customer?.City ?? string.Empty,
            State = profile?.State ?? customer?.State ?? string.Empty,
            ZipCode = profile?.ZipCode ?? customer?.PostalCode ?? string.Empty,
            Email = current.Email ?? user?.Email ?? customer?.Email ?? string.Empty
        };
    }

    public async Task<CustomerAccountViewModel> SaveAsync(CustomerAccountViewModel model)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to update your account.";
            return model;
        }

        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        var displayName = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

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

        profile.FirstName = firstName;
        profile.LastName = lastName;
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

        customer.FirstName = firstName;
        customer.LastName = lastName;
        customer.Phone = profile.Phone;
        customer.AddressLine1 = profile.Address1;
        customer.AddressLine2 = profile.Address2;
        customer.City = profile.City;
        customer.State = profile.State;
        customer.PostalCode = profile.ZipCode;
        customer.Email = string.IsNullOrWhiteSpace(current.Email) ? customer.Email : current.Email.Trim();

        var user = await _userManager.FindByIdAsync(current.UserId);
        if (user is not null)
        {
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName;

            if (!string.IsNullOrWhiteSpace(current.Email))
            {
                user.Email = current.Email.Trim();
                user.NormalizedEmail = current.Email.Trim().ToUpperInvariant();
                user.UserName = current.Email.Trim();
                user.NormalizedUserName = current.Email.Trim().ToUpperInvariant();
            }

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
        }

        await db.SaveChangesAsync();

        var refreshed = await GetAsync();
        refreshed.SuccessMessage = "Account details updated.";
        return refreshed;
    }
}