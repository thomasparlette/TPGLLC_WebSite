using System.Security.Claims;

namespace TPGLLC.Web.Infrastructure;

public static class PortalRouteHelper
{
    public static string GetPortalHomeRoute(ClaimsPrincipal user)
    {
        if (user.IsInRole("Administrator"))
        {
            return "/portal/admin";
        }

        if (user.IsInRole("ServiceAdvisor"))
        {
            return "/portal/service-advisor";
        }

        if (user.IsInRole("Technician"))
        {
            return "/portal/technician";
        }

        if (user.IsInRole("Finance"))
        {
            return "/portal//finance";
        }


        return "/portal/customer";
    }

    public static string GetPortalLabel(ClaimsPrincipal user)
    {
        if (user.IsInRole("Administrator"))
        {
            return "Administration";
        }

        if (user.IsInRole("ServiceAdvisor"))
        {
            return "Service Advisor";
        }

        if (user.IsInRole("Technician"))
        {
            return "Technician";
        }

        if (user.IsInRole("Finance"))
        {
            return "Finance";
        }


        if (user.IsInRole("Customer"))
        {
            return "Customer Portal";
        }

        return "Portal";
    }
}
