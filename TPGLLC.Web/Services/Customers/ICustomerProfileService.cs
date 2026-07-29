using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Services.Customers;

public interface ICustomerProfileService
{
    Task<CustomerProfile?> GetCurrentAsync();

    Task<CustomerProfile?> GetAsync(string userId);

    Task<CustomerProfile> CreateAsync(string userId);

    Task UpdateAsync(CustomerProfile profile);
}