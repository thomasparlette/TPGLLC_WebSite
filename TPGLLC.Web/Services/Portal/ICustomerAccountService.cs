using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public interface ICustomerAccountService
{
    Task<CustomerAccountViewModel> GetAsync();
    Task<CustomerAccountViewModel> SaveAsync(CustomerAccountViewModel model);
}