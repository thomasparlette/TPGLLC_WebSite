namespace TPGLLC.Web.ViewModels.Portal;

public sealed record WorkOrderStatusDefinition(
    string Key,
    string Label,
    string CssClass,
    string Description);

public static class WorkOrderStatusCatalog
{
    public static IReadOnlyList<WorkOrderStatusDefinition> BoardColumns { get; } =
    [
        new("Requested", "Requested", "workorder-status-requested", "New customer request"),
        new("Quoted", "Estimate Ready", "workorder-status-quoted", "Estimate is being prepared"),
        new("Waiting on Customer Approval", "Awaiting Approval", "workorder-status-waiting", "Customer decision needed"),
        new("Approved", "Approved", "workorder-status-approved", "Approved for repair"),
        new("In Progress", "In Progress", "workorder-status-in-progress", "Technician is working"),
        new("Completed", "Completed", "workorder-status-completed", "Repair is complete"),
        new("Invoiced", "Invoiced", "workorder-status-invoiced", "Invoice is ready"),
        new("Closed", "Closed", "workorder-status-closed", "Repair order is closed")
    ];

    public static IReadOnlyList<string> AllStatuses { get; } =
    [
        "Requested",
        "Quoted",
        "Waiting on Customer Approval",
        "Approved",
        "In Progress",
        "Completed",
        "Invoiced",
        "Declined",
        "Cancelled",
        "Closed"
    ];

    public static IReadOnlyList<string> TechnicianStatuses { get; } =
    [
        "Requested",
        "In Progress",
        "Completed"
    ];

    public static WorkOrderStatusDefinition GetDefinition(string? status)
    {
        var normalized = status?.Trim();

        if (string.Equals(normalized, "Declined", StringComparison.OrdinalIgnoreCase))
        {
            return new("Declined", "Declined", "workorder-status-declined", "Customer declined proposed work");
        }

        if (string.Equals(normalized, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return new("Cancelled", "Canceled", "workorder-status-cancelled", "Repair order was canceled");
        }

        return BoardColumns.FirstOrDefault(x => string.Equals(x.Key, normalized, StringComparison.OrdinalIgnoreCase))
            ?? BoardColumns[0];
    }

    public static string GetBoardColumnKey(string? status)
    {
        if (string.Equals(status, "Declined", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "Closed";
        }

        return GetDefinition(status).Key;
    }

    public static bool IsClosed(string? status) =>
        string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Invoiced", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Declined", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase);
}
