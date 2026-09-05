namespace TPGLLC.Web.Integration;

public sealed record RepairSnapshot(
    int WorkOrderId, Guid CustomerId, string Number, string Vehicle,
    string Status, DateOnly ServiceDate, string? CustomerUpdate,
    decimal Estimate, IReadOnlyList<RepairPart> Parts);

public sealed record RepairPart(int Id, string Description, decimal Quantity, decimal UnitPrice);
