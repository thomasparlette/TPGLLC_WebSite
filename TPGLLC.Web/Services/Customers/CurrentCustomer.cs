namespace TPGLLC.Web.Services.Customers;

public sealed class CurrentCustomer
{
    public string UserId { get; init; } = "";

    public string Email { get; init; } = "";

    public bool IsAuthenticated { get; init; }

    public bool IsCustomer { get; init; }

    public bool IsEmployee { get; init; }

    public bool IsAdministrator { get; init; }
}