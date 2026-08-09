using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly IEmailSender _emailSender;
    private readonly NavigationManager _navigationManager;
    private readonly IPortalSessionState _portalSessionState;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerAccountService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IEmailSender emailSender,
        NavigationManager navigationManager,
        IPortalSessionState portalSessionState,
        UserManager<ApplicationUser> userManager)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _emailSender = emailSender;
        _navigationManager = navigationManager;
        _portalSessionState = portalSessionState;
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
            Phone = user?.PhoneNumber ?? profile?.Phone ?? customer?.Phone ?? string.Empty,
            AddressLine1 = profile?.Address1 ?? customer?.AddressLine1 ?? string.Empty,
            AddressLine2 = profile?.Address2 ?? customer?.AddressLine2 ?? string.Empty,
            City = profile?.City ?? customer?.City ?? string.Empty,
            State = profile?.State ?? customer?.State ?? string.Empty,
            ZipCode = profile?.ZipCode ?? customer?.PostalCode ?? string.Empty,
            Email = user?.Email ?? customer?.Email ?? current.Email,
            EmailConfirmed = user?.EmailConfirmed ?? false
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
        var email = model.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

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
        profile.Phone = phone;
        profile.Address1 = string.IsNullOrWhiteSpace(model.AddressLine1) ? null : model.AddressLine1.Trim();
        profile.Address2 = string.IsNullOrWhiteSpace(model.AddressLine2) ? null : model.AddressLine2.Trim();
        profile.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        profile.State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim();
        profile.ZipCode = string.IsNullOrWhiteSpace(model.ZipCode) ? null : model.ZipCode.Trim();
        profile.UpdatedUtc = DateTimeOffset.UtcNow;

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
        customer.Phone = phone;
        customer.AddressLine1 = profile.Address1;
        customer.AddressLine2 = profile.Address2;
        customer.City = profile.City;
        customer.State = profile.State;
        customer.PostalCode = profile.ZipCode;
        customer.Email = string.IsNullOrWhiteSpace(email) ? customer.Email : email;
        customer.UpdatedUtc = DateTimeOffset.UtcNow;

        var user = await _userManager.FindByIdAsync(current.UserId);
        var originalEmail = user?.Email?.Trim() ?? current.Email.Trim();
        var emailChanged = !string.IsNullOrWhiteSpace(email) &&
                           !string.Equals(originalEmail, email, StringComparison.OrdinalIgnoreCase);

        if (user is not null)
        {
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName;
            user.PhoneNumber = phone;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                model.ErrorMessage = string.Join(" ", updateResult.Errors.Select(x => x.Description));
                return model;
            }
        }

        await db.SaveChangesAsync();

        _portalSessionState.SetDisplayName(displayName);
        if (!emailChanged)
        {
            _portalSessionState.SetEmail(email);
        }

        var refreshed = await GetAsync();

        if (emailChanged && user is not null)
        {
            var sendResult = await SendEmailChangeConfirmationAsync(user, email);

            refreshed.Email = email;
            refreshed.EmailConfirmed = false;
            refreshed.SuccessMessage = sendResult.Succeeded
                ? $"Account details updated. A verification email has been sent to {email}."
                : "Account details updated. The email verification message could not be sent.";
            refreshed.ErrorMessage = sendResult.Succeeded ? null : sendResult.ErrorMessage;
            return refreshed;
        }

        refreshed.SuccessMessage = "Account details updated.";
        return refreshed;
    }

    private async Task<EmailSendResult> SendEmailChangeConfirmationAsync(
        ApplicationUser user,
        string newEmail)
    {
        try
        {
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var callbackUrl = _navigationManager.ToAbsoluteUri(
                $"/Identity/Account/ConfirmEmailChange" +
                $"?userId={Uri.EscapeDataString(user.Id)}" +
                $"&email={Uri.EscapeDataString(newEmail)}" +
                $"&token={Uri.EscapeDataString(encodedToken)}").ToString();

            var subject = "Confirm your new email address";
            var body = $"""
                <p>Your email address was updated for your customer account.</p>
                <p>Please confirm it by <a href="{callbackUrl}">clicking here</a>.</p>
                """;

            await _emailSender.SendEmailAsync(newEmail, subject, body);

            return new EmailSendResult(true, null);
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ex.Message);
        }
    }

    private sealed record EmailSendResult(bool Succeeded, string? ErrorMessage);
}
