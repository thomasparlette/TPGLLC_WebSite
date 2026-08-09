using System.Security.Claims;

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

        var displayName =
            user.FindFirst("display_name")?.Value ??
            user.Identity?.Name ??
            user.FindFirst(ClaimTypes.Email)?.Value ??
            string.Empty;

        return new CurrentCustomer
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName = displayName,
            IsCustomer = user.IsInRole("Customer"),
            IsServiceAdvisor = user.IsInRole("ServiceAdvisor"),
            IsTechnician = user.IsInRole("Technician"),
            IsFinance = user.IsInRole("Finance"),
            IsAdministrator = user.IsInRole("Administrator")
        };
    }
}

