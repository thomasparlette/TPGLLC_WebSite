namespace TPGLLC.Web.Services.Customers;

public sealed class CurrentCustomer
{
    public bool IsAuthenticated { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsCustomer { get; init; }
    public bool IsEmployee { get; init; }
    public bool IsAdministrator { get; init; }
}