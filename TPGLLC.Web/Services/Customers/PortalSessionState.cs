namespace TPGLLC.Web.Services.Customers;

public interface IPortalSessionState
{
    string? DisplayName { get; }
    event Action? Changed;
    void SetDisplayName(string? displayName);
    void Clear();
}

public sealed class PortalSessionState : IPortalSessionState
{
    public event Action? Changed;

    public string? DisplayName { get; private set; }

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

    public void Clear()
    {
        if (DisplayName is null)
        {
            return;
        }

        DisplayName = null;
        Changed?.Invoke();
    }
}
