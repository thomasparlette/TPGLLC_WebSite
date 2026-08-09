using System.Security.Claims;

namespace TPGLLC.Web.Authorization;

public static class PortalNavigationHelper
{
    public const string CustomerDashboardPath = "/portal/dashboard";
    public const string AdministratorDashboardPath = "/portal/admin";
    public const string ServiceAdvisorDashboardPath = "/portal/employee/service-advisor";
    public const string TechnicianDashboardPath = "/portal/employee/technician";
    public const string FinanceDashboardPath = "/portal/employee/finance";

    // Legacy alias kept for callers that still think in terms of a generic employee landing page.
    public const string EmployeeDashboardPath = ServiceAdvisorDashboardPath;

    public static string GetDefaultPortalPath(ClaimsPrincipal user)
    {
        if (user.IsAdministrator())
        {
            return AdministratorDashboardPath;
        }

        if (user.IsServiceAdvisor())
        {
            return ServiceAdvisorDashboardPath;
        }

        if (user.IsTechnician())
        {
            return TechnicianDashboardPath;
        }

        if (user.IsFinance())
        {
            return FinanceDashboardPath;
        }

        return CustomerDashboardPath;
    }

    public static string GetDefaultPortalPath(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleSet.Contains("Administrator"))
        {
            return AdministratorDashboardPath;
        }

        if (roleSet.Contains("ServiceAdvisor") || roleSet.Contains("Employee"))
        {
            return ServiceAdvisorDashboardPath;
        }

        if (roleSet.Contains("Technician"))
        {
            return TechnicianDashboardPath;
        }

        if (roleSet.Contains("Finance"))
        {
            return FinanceDashboardPath;
        }

        return CustomerDashboardPath;
    }
}
