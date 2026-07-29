using System.Security.Claims;

namespace TPGLLC.Web.Services.Customers;

public interface ICurrentCustomerAccessor
{
    CurrentCustomer GetCurrentCustomer();
}

public sealed class CurrentCustomerAccessor
    : ICurrentCustomerAccessor
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
            return new CurrentCustomer();

        return new CurrentCustomer
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,

            UserId =
                user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "",

            Email =
                user.FindFirstValue(ClaimTypes.Email)
                ?? "",

            IsCustomer =
                user.IsInRole("Customer"),

            IsEmployee =
                user.IsInRole("Employee"),

            IsAdministrator =
                user.IsInRole("Administrator")
        };
    }
}