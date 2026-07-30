using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Services.Customers;

public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _current;

    public CustomerProfileService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor current)
    {
        _dbFactory = dbFactory;
        _current = current;
    }

    public async Task<CustomerProfile?> GetCurrentAsync()
    {
        var current = _current.GetCurrentCustomer();

        if (!current.IsAuthenticated)
        {
            return null;
        }

        return await GetAsync(current.UserId);
    }

    public async Task<CustomerProfile?> GetAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);
    }

    public async Task<CustomerProfile> CreateAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.CustomerProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);

        if (existing is not null)
        {
            return existing;
        }

        var profile = new CustomerProfile
        {
            ApplicationUserId = userId,
            ReceiveEmail = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        db.CustomerProfiles.Add(profile);
        await db.SaveChangesAsync();

        return profile;
    }

    public async Task<CustomerProfile> SaveAsync(CustomerProfile profile)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.CustomerProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == profile.ApplicationUserId);

        if (existing is null)
        {
            profile.CreatedUtc = DateTimeOffset.UtcNow;
            db.CustomerProfiles.Add(profile);
            await db.SaveChangesAsync();
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

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task UpdateAsync(CustomerProfile profile)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        profile.UpdatedUtc = DateTimeOffset.UtcNow;
        db.CustomerProfiles.Update(profile);

        await db.SaveChangesAsync();
    }
}