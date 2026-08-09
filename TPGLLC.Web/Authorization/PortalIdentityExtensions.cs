using System.Security.Claims;

namespace TPGLLC.Web.Authorization;

public static class PortalIdentityExtensions
{
    public static bool IsAdministrator(this ClaimsPrincipal user) =>
        user.IsInRole(PortalPolicies.Administrator);

    public static bool IsCustomerOnly(this ClaimsPrincipal user) =>
        user.IsInRole(PortalPolicies.Customer)
        && !user.IsAdministrator()
        && !user.IsServiceAdvisor()
        && !user.IsTechnician()
        && !user.IsFinance();

    public static bool IsServiceAdvisor(this ClaimsPrincipal user) =>
        user.IsInRole(PortalPolicies.ServiceAdvisor);

    public static bool IsTechnician(this ClaimsPrincipal user) =>
        user.IsInRole(PortalPolicies.Technician);

    public static bool IsFinance(this ClaimsPrincipal user) =>
        user.IsInRole(PortalPolicies.Finance);

    public static bool IsEmployeePortalUser(this ClaimsPrincipal user) =>
        user.IsServiceAdvisor() || user.IsTechnician() || user.IsFinance();

    public static string GetPortalHomeHref(this ClaimsPrincipal user)
    {
        if (user.IsAdministrator())
        {
            return PortalNavigationHelper.AdministratorDashboardPath;
        }

        if (user.IsServiceAdvisor())
        {
            return PortalNavigationHelper.ServiceAdvisorDashboardPath;
        }

        if (user.IsTechnician())
        {
            return PortalNavigationHelper.TechnicianDashboardPath;
        }

        if (user.IsFinance())
        {
            return PortalNavigationHelper.FinanceDashboardPath;
        }

        if (user.IsInRole(PortalPolicies.Customer))
        {
            return PortalNavigationHelper.CustomerDashboardPath;
        }

        return "/";
    }

    public static string GetPortalSubtitle(this ClaimsPrincipal user)
    {
        if (user.IsAdministrator())
        {
            return "Administration Portal";
        }

        if (user.IsServiceAdvisor())
        {
            return "Service Advisor Portal";
        }

        if (user.IsTechnician())
        {
            return "Technician Portal";
        }

        if (user.IsFinance())
        {
            return "Finance Portal";
        }

        if (user.IsInRole(PortalPolicies.Customer))
        {
            return "Customer Portal";
        }

        return "Portal";
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        var displayName =
            user.FindFirst("display_name")?.Value ??
            user.Identity?.Name ??
            user.FindFirst(ClaimTypes.Email)?.Value ??
            "Customer";

        return string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName;
    }

    public static string GetInitials(this ClaimsPrincipal user)
    {
        var name = user.GetDisplayName();
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "C";
        }

        if (parts.Length == 1)
        {
            return parts[0].Substring(0, 1).ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
