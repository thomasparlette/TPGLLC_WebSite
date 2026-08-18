using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public sealed class WorkOrderPortalService : IWorkOrderPortalService
{
    private static readonly HashSet<string> ClosedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Completed",
        "Cancelled",
        "Declined",
        "Closed"
    };

    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkOrderPortalService(IDbContextFactory<TPGLLCDbContext> dbFactory, ICurrentCustomerAccessor currentCustomerAccessor, IHttpContextAccessor httpContextAccessor)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkOrderPageViewModel> GetCustomerAsync()
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You must be signed in to view work orders."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        if (customer is null)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "Customer record not found."
            };
        }

        await EnsureWorkOrderNumbersAsync(db, customer.Id);

        var workOrders = await db.ServiceHistoryEntries
            .AsNoTracking()
            .Include(x => x.Parts)
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        await ApplyAppointmentFieldsAsync(db, workOrders);

        return new WorkOrderPageViewModel
        {
            WorkOrders = workOrders,
            CanEdit = false
        };
    }

    public async Task<WorkOrderPageViewModel> GetStaffWorkOrdersAsync()
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You must be signed in to manage work orders."
            };
        }

        if (!IsServiceAdvisorOrAdministrator())
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You are not authorized to manage work orders."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        await EnsureWorkOrderNumbersAsync(db);

        var workOrders = await db.ServiceHistoryEntries
            .AsNoTracking()
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        await ApplyAppointmentFieldsAsync(db, workOrders);

        return new WorkOrderPageViewModel
        {
            WorkOrders = workOrders,
            CanEdit = true
        };
    }

    public async Task<WorkOrderPageViewModel> StartEditAsync(Guid workOrderId)
    {
        var model = await GetStaffWorkOrdersAsync();
        if (!string.IsNullOrWhiteSpace(model.ErrorMessage))
        {
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var selectedOrder = await db.ServiceHistoryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == workOrderId);

        if (selectedOrder is null)
        {
            model.ErrorMessage = "Work order not found.";
            return model;
        }

        model.EditingWorkOrderId = selectedOrder.Id;
        var appointment = selectedOrder.AppointmentRequestId.HasValue
            ? await db.AppointmentRequests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == selectedOrder.AppointmentRequestId.Value)
            : null;
        model.Form = MapToForm(selectedOrder, appointment);
        model.Form.Parts = await db.ServiceHistoryParts.AsNoTracking()
            .Where(x => x.ServiceHistoryEntryId == selectedOrder.Id)
            .OrderBy(x => x.Description)
            .Select(x => new WorkOrderPartEditViewModel
            {
                Id = x.Id,
                Description = x.Description,
                Quantity = x.Quantity.ToString("0.##"),
                UnitPrice = x.UnitPrice.HasValue ? x.UnitPrice.Value.ToString("0.00") : null,
                IsApplied = x.IsApplied,
                IsApproved = x.IsApproved
            }).ToListAsync();
        return model;
    }

    public async Task<WorkOrderPageViewModel> ResetAsync()
    {
        return await GetStaffWorkOrdersAsync();
    }

    public async Task<WorkOrderPageViewModel> SaveAsync(WorkOrderPageViewModel model)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to manage work orders.";
            return model;
        }

        if (!IsServiceAdvisorOrAdministrator())
        {
            model.ErrorMessage = "You are not authorized to manage work orders.";
            return model;
        }

        if (model.EditingWorkOrderId is null)
        {
            model.ErrorMessage = "Select an existing work order to update.";
            return model;
        }

        if (!TryParseServiceDate(model.Form.ServiceDate, out var serviceDate))
        {
            model.ErrorMessage = "Service date is required and must be valid.";
            return model;
        }

        if (!TryParseMileage(model.Form.Mileage, out var mileage))
        {
            model.ErrorMessage = "Mileage must be a whole number.";
            return model;
        }

        if (!TryParseMileage(model.Form.MileageOut, out var mileageOut))
        {
            model.ErrorMessage = "Mileage out must be a whole number.";
            return model;
        }

        if (!TryParseDecimal(model.Form.LaborAmount, out var laborAmount))
        {
            model.ErrorMessage = "Labor amount must be a valid number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await db.ServiceHistoryEntries.FirstOrDefaultAsync(x => x.Id == model.EditingWorkOrderId.Value);

        if (workOrder is null)
        {
            model.ErrorMessage = "Work order not found.";
            return model;
        }

        var requestedWorkOrderNumber = Normalize(model.Form.WorkOrderNumber);
        if (string.IsNullOrWhiteSpace(requestedWorkOrderNumber))
        {
            requestedWorkOrderNumber = await GetNextWorkOrderNumberAsync(db);
        }
        else
        {
            var duplicateNumber = await db.ServiceHistoryEntries
                .AsNoTracking()
                .AnyAsync(x => x.Id != workOrder.Id && x.WorkOrderNumber != null &&
                    x.WorkOrderNumber.ToUpper() == requestedWorkOrderNumber.ToUpper());

            if (duplicateNumber)
            {
                model.ErrorMessage = $"Work order number '{requestedWorkOrderNumber}' is already in use.";
                return model;
            }
        }

        workOrder.WorkOrderNumber = requestedWorkOrderNumber;
        workOrder.VehicleName = Normalize(model.Form.VehicleName) ?? workOrder.VehicleName;
        workOrder.ServiceDate = DateOnly.FromDateTime(serviceDate);
        var appointment = workOrder.AppointmentRequestId.HasValue
            ? await db.AppointmentRequests.FirstOrDefaultAsync(x => x.RequestId == workOrder.AppointmentRequestId.Value)
            : null;
        if (appointment is not null)
        {
            workOrder.Service = Normalize(appointment.ServiceNeeded) ?? workOrder.Service;
            workOrder.Complaint = Normalize(appointment.Message);
            workOrder.ApprovalStatus = Normalize(appointment.Status);
        }
        workOrder.Technician = Normalize(model.Form.Technician);
        workOrder.Status = Normalize(model.Form.Status) ?? workOrder.Status;
        workOrder.Mileage = mileage;
        workOrder.MileageOut = mileageOut;
        workOrder.LaborAmount = laborAmount;
        workOrder.InvoiceNumber = Normalize(model.Form.InvoiceNumber);
        workOrder.Notes = Normalize(model.Form.Notes);
        workOrder.InternalNotes = Normalize(model.Form.InternalNotes);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;

        var existingParts = await db.ServiceHistoryParts
            .Where(x => x.ServiceHistoryEntryId == workOrder.Id)
            .ToListAsync();
        var existingById = existingParts.ToDictionary(x => x.Id);
        var appliedPartsTotal = 0m;
        var approvedPartsTotal = 0m;
        foreach (var part in model.Form.Parts.Where(x => !string.IsNullOrWhiteSpace(x.Description)))
        {
            if (!TryParseDecimal(part.Quantity, out var quantity) || quantity is null || quantity <= 0)
            {
                model.ErrorMessage = "Part quantities must be greater than zero.";
                return model;
            }
            if (!TryParseDecimal(part.UnitPrice, out var unitPrice))
            {
                model.ErrorMessage = "Part prices must be valid numbers.";
                return model;
            }

            var savedPart = existingById.TryGetValue(part.Id, out var existing)
                ? existing
                : new ServiceHistoryPart { ServiceHistoryEntryId = workOrder.Id };
            savedPart.Description = part.Description.Trim();
            savedPart.Quantity = quantity.Value;
            savedPart.UnitPrice = unitPrice;
            savedPart.IsApplied = part.IsApplied;
            savedPart.IsApproved = string.Equals(workOrder.Status, "Quoted", StringComparison.OrdinalIgnoreCase)
                ? part.IsApproved
                : savedPart.IsApproved;
            var partTotal = savedPart.Quantity * (savedPart.UnitPrice ?? 0m);
            if (savedPart.IsApplied) appliedPartsTotal += partTotal;
            if (savedPart.IsApproved) approvedPartsTotal += partTotal;
            if (existing is null) db.ServiceHistoryParts.Add(savedPart);
        }
        var submittedIds = model.Form.Parts.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToHashSet();
        db.ServiceHistoryParts.RemoveRange(existingParts.Where(x => !submittedIds.Contains(x.Id)));

        var laborTotal = laborAmount ?? 0m;
        workOrder.EstimateAmount = appliedPartsTotal + laborTotal;
        workOrder.InvoiceAmount = approvedPartsTotal + laborTotal;

        await db.SaveChangesAsync();

        var refreshed = await GetStaffWorkOrdersAsync();
        refreshed.SuccessMessage = $"Work order {requestedWorkOrderNumber} updated.";
        return refreshed;
    }

    public async Task<WorkOrderPageViewModel> ApprovePartAsync(Guid workOrderId, Guid partId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "You must be signed in to approve work." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await GetCustomerWorkOrderAsync(db, workOrderId, current.UserId);
        if (workOrder is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Work order not found." };
        }

        if (!IsWaitingForCustomerApproval(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not waiting for customer approval.");
        }

        var part = workOrder.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Part not found." };
        }

        part.IsApproved = true;
        workOrder.Status = "In Progress";
        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Part approved and work is now in progress.");
    }

    public async Task<WorkOrderPageViewModel> ApproveWorkOrderAsync(Guid workOrderId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "You must be signed in to approve work." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await GetCustomerWorkOrderAsync(db, workOrderId, current.UserId);
        if (workOrder is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Work order not found." };
        }

        if (!IsWaitingForCustomerApproval(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not waiting for customer approval.");
        }

        foreach (var part in workOrder.Parts)
        {
            part.IsApproved = true;
        }

        workOrder.Status = "In Progress";
        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Work order approved and now in progress.");
    }

    private static async Task EnsureWorkOrderNumbersAsync(TPGLLCDbContext db, Guid? customerId = null)
    {
        var query = db.ServiceHistoryEntries.AsQueryable();
        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        var entries = await query
            .OrderBy(x => x.CreatedUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var changed = false;
        var nextNumber = await GetNextSequenceNumberAsync(db);

        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.WorkOrderNumber))
            {
                continue;
            }

            entry.WorkOrderNumber = FormatWorkOrderNumber(nextNumber++);
            entry.UpdatedUtc = DateTimeOffset.UtcNow;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    private async Task<WorkOrderPageViewModel> GetCustomerResultAsync(string message)
    {
        var result = await GetCustomerAsync();
        result.SuccessMessage = message;
        return result;
    }

    private async Task<WorkOrderPageViewModel> GetCustomerErrorAsync(string message)
    {
        var result = await GetCustomerAsync();
        result.ErrorMessage = message;
        return result;
    }

    private static async Task<ServiceHistoryEntry?> GetCustomerWorkOrderAsync(TPGLLCDbContext db, Guid workOrderId, string userId)
    {
        return await db.ServiceHistoryEntries
            .Include(x => x.Parts)
            .Where(x => x.Id == workOrderId && x.Customer != null && x.Customer.ApplicationUserId == userId)
            .FirstOrDefaultAsync();
    }

    private static bool IsWaitingForCustomerApproval(string? status) =>
        string.Equals(status, "Waiting on Customer Approval", StringComparison.OrdinalIgnoreCase);

    private static decimal CalculateApprovedTotal(ServiceHistoryEntry workOrder) =>
        workOrder.Parts.Where(x => x.IsApproved)
            .Sum(x => x.Quantity * (x.UnitPrice ?? 0m)) + (workOrder.LaborAmount ?? 0m);

    private static async Task<string> GetNextWorkOrderNumberAsync(TPGLLCDbContext db)
    {
        var nextSequence = await GetNextSequenceNumberAsync(db);
        return FormatWorkOrderNumber(nextSequence);
    }

    private static async Task<int> GetNextSequenceNumberAsync(TPGLLCDbContext db)
    {
        var numbers = await db.ServiceHistoryEntries
            .AsNoTracking()
            .Where(x => x.WorkOrderNumber != null)
            .Select(x => x.WorkOrderNumber!)
            .ToListAsync();

        var max = 0;
        foreach (var number in numbers)
        {
            if (TryExtractSequence(number, out var value) && value > max)
            {
                max = value;
            }
        }

        return max + 1;
    }

    private static string FormatWorkOrderNumber(int sequence)
        => $"WO-{DateTime.UtcNow:yyyy}-{sequence:D5}";

    private static bool TryExtractSequence(string value, out int sequence)
    {
        sequence = 0;
        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 3 && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            sequence = parsed;
            return true;
        }

        return false;
    }

    private static WorkOrderEditViewModel MapToForm(ServiceHistoryEntry entry, AppointmentRequest? appointment = null)
    {
        return new WorkOrderEditViewModel
        {
            Id = entry.Id,
            WorkOrderNumber = entry.WorkOrderNumber ?? string.Empty,
            ServiceDate = entry.ServiceDate.ToDateTime(TimeOnly.MinValue),
            VehicleName = entry.VehicleName,
            Service = appointment?.ServiceNeeded ?? entry.Service,
            Complaint = appointment?.Message ?? entry.Complaint,
            Diagnosis = entry.Diagnosis,
            Technician = entry.Technician,
            Status = entry.Status,
            ApprovalStatus = appointment?.Status ?? entry.ApprovalStatus,
            Mileage = entry.Mileage?.ToString(CultureInfo.InvariantCulture),
            MileageOut = entry.MileageOut?.ToString(CultureInfo.InvariantCulture),
            EstimateAmount = entry.EstimateAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            LaborAmount = entry.LaborAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            InvoiceNumber = entry.InvoiceNumber,
            InvoiceAmount = entry.InvoiceAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            Notes = entry.Notes,
            InternalNotes = entry.InternalNotes
        };
    }

    private static async Task ApplyAppointmentFieldsAsync(TPGLLCDbContext db, List<ServiceHistoryEntry> workOrders)
    {
        var appointmentIds = workOrders.Where(x => x.AppointmentRequestId.HasValue)
            .Select(x => x.AppointmentRequestId!.Value).ToList();
        if (appointmentIds.Count == 0) return;

        var appointments = await db.AppointmentRequests.AsNoTracking()
            .Where(x => appointmentIds.Contains(x.RequestId))
            .ToDictionaryAsync(x => x.RequestId);
        foreach (var workOrder in workOrders)
        {
            if (workOrder.AppointmentRequestId is not Guid appointmentId || !appointments.TryGetValue(appointmentId, out var appointment)) continue;
            workOrder.Service = appointment.ServiceNeeded ?? workOrder.Service;
            workOrder.Complaint = appointment.Message;
            workOrder.ApprovalStatus = appointment.Status;
        }
    }
    private bool IsServiceAdvisorOrAdministrator()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("ServiceAdvisor") == true || user?.IsInRole("Administrator") == true;
    }
    private static bool TryParseServiceDate(DateTime value, out DateTime parsed)
    {
        parsed = value;
        return value != default;
    }

    private static bool TryParseMileage(string? value, out int? mileage)
    {
        mileage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Replace(",", string.Empty, StringComparison.Ordinal).Trim();
        if (int.TryParse(normalized, out var parsed) && parsed >= 0)
        {
            mileage = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseDecimal(string? value, out decimal? parsed)
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount >= 0)
        {
            parsed = amount;
            return true;
        }

        return false;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
