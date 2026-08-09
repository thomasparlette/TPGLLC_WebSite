using System.Security.Claims;
using TPGLLC.Web.Authorization;

namespace TPGLLC.Web.Services.Customers;

public sealed class CurrentCustomerAccessor : ICurrentCustomerAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentCustomerAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public CurrentCustomer GetCurrentCustomer()
    {
        var user = _http.HttpContext?.User;

        if (user == null)
        {
            return new CurrentCustomer();
        }

        return new CurrentCustomer
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName = user.GetDisplayName(),
            IsCustomer = user.IsInRole(PortalPolicies.Customer),
            IsEmployee = user.IsEmployeePortalUser(),
            IsAdministrator = user.IsAdministrator()
        };
    }
}
