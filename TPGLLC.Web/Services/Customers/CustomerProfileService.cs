using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Services.Customers;

public sealed class CustomerProfileService
    : ICustomerProfileService
{
    private readonly TPGLLCDbContext _db;

    private readonly ICurrentCustomerAccessor _current;

    public CustomerProfileService(
        TPGLLCDbContext db,
        ICurrentCustomerAccessor current)
    {
        _db = db;
        _current = current;
    }

    public async Task<CustomerProfile?> GetCurrentAsync()
    {
        var current = _current.GetCurrentCustomer();

        if (!current.IsAuthenticated)
            return null;

        return await GetAsync(current.UserId);
    }

    public async Task<CustomerProfile?> GetAsync(string userId)
    {
        return await _db.CustomerProfiles
            .FirstOrDefaultAsync(x =>
                x.ApplicationUserId == userId);
    }

    public async Task<CustomerProfile> CreateAsync(string userId)
    {
        var existing = await GetAsync(userId);

        if (existing != null)
            return existing;

        var profile = new CustomerProfile
        {
            ApplicationUserId = userId
        };

        _db.CustomerProfiles.Add(profile);

        await _db.SaveChangesAsync();

        return profile;
    }

    public async Task UpdateAsync(CustomerProfile profile)
    {
        profile.UpdatedUtc = DateTimeOffset.UtcNow;

        _db.Update(profile);

        await _db.SaveChangesAsync();
    }
    public async Task<CustomerProfile> SaveAsync(CustomerProfile profile)
    {
        var existing = await GetAsync(profile.ApplicationUserId);

        if (existing is null)
        {
            profile.CreatedUtc = DateTimeOffset.UtcNow;
            _db.CustomerProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        existing.FirstName = profile.FirstName;
        existing.LastName = profile.LastName;
        existing.Phone = profile.Phone;
        existing.Company = profile.Company;
        existing.Address1 = profile.Address1;
        existing.Address2 = profile.Address2;
        existing.City = profile.City;
        existing.State = profile.State;
        existing.ZipCode = profile.ZipCode;
        existing.Country = profile.Country;
        existing.PreferredContactMethod = profile.PreferredContactMethod;
        existing.ReceiveEmail = profile.ReceiveEmail;
        existing.ReceiveSms = profile.ReceiveSms;
        existing.UpdatedUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }
}