using System.Security.Claims;

namespace TPGLLC.Web.Services.Customers;

public sealed class CurrentCustomerAccessor : ICurrentCustomerAccessor
{
    private readonly IHttpContextAccessor _http;
    private readonly IPortalSessionState _portalSessionState;

    public CurrentCustomerAccessor(IHttpContextAccessor http, IPortalSessionState portalSessionState)
    {
        _http = http;
        _portalSessionState = portalSessionState;
    }

    public CurrentCustomer GetCurrentCustomer()
    {
        var user = _http.HttpContext?.User;

        if (user == null)
        {
            return new CurrentCustomer();
        }

        var displayName =
            _portalSessionState.DisplayName ??
            user.FindFirst("display_name")?.Value ??
            user.Identity?.Name ??
            user.FindFirst(ClaimTypes.Email)?.Value ??
            string.Empty;

        var email =
            _portalSessionState.Email ??
            user.FindFirstValue(ClaimTypes.Email) ??
            string.Empty;

        return new CurrentCustomer
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = email,
            DisplayName = displayName,
            IsCustomer = user.IsInRole("Customer"),
            IsEmployee = user.IsInRole("Employee") || user.IsInRole("ServiceAdvisor") || user.IsInRole("Technician") || user.IsInRole("Finance"),
            IsAdministrator = user.IsInRole("Administrator")
        };
    }
}
