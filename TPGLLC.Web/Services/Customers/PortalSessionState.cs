namespace TPGLLC.Web.Services.Customers;

public interface IPortalSessionState
{
    string? DisplayName { get; }
    string? Email { get; }
    event Action? Changed;
    void SetDisplayName(string? displayName);
    void SetEmail(string? email);
    void Clear();
}

public sealed class PortalSessionState : IPortalSessionState
{
    public event Action? Changed;

    public string? DisplayName { get; private set; }

    public string? Email { get; private set; }

    public void SetDisplayName(string? displayName)
    {
        var normalized = string.IsNullOrWhiteSpace(displayName)
            ? null
            : displayName.Trim();

        if (string.Equals(DisplayName, normalized, StringComparison.Ordinal))
        {
            return;
        }

        DisplayName = normalized;
        Changed?.Invoke();
    }

    public void SetEmail(string? email)
    {
        var normalized = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim();

        if (string.Equals(Email, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Email = normalized;
        Changed?.Invoke();
    }

    public void Clear()
    {
        var hadValue = DisplayName is not null || Email is not null;

        if (!hadValue)
        {
            return;
        }

        DisplayName = null;
        Email = null;
        Changed?.Invoke();
    }
}
