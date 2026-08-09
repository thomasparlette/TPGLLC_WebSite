using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Services.Vehicles;
using TPGLLC.Web.Components.PortalShared.Appointments;
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

    public WorkOrderPortalService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
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

        var workOrders = await db.ServiceHistoryEntries
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        return new WorkOrderPageViewModel
        {
            WorkOrders = workOrders,
            AppointmentRequests = [],
            CanEdit = false
        };
    }

    public async Task<WorkOrderPageViewModel> GetEmployeeAsync()
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You must be signed in to manage work orders."
            };
        }

        if (!current.IsEmployee && !current.IsAdministrator)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You are not authorized to manage work orders."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var workOrders = await db.ServiceHistoryEntries
            .AsNoTracking()
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        var appointmentRequests = await db.AppointmentRequests
    .AsNoTracking()
    .Where(x =>
        x.Status == null ||
        x.Status == "" ||
        x.Status == "Pending" ||
        x.Status == "Requested")
    .OrderByDescending(x => x.SubmittedAtUtc)
    .ToListAsync();

        return new WorkOrderPageViewModel
        {
            WorkOrders = workOrders,
            AppointmentRequests = appointmentRequests,
            CanEdit = true
        };
    }

    public async Task<WorkOrderPageViewModel> StartEditAsync(Guid workOrderId)
    {
        var model = await GetEmployeeAsync();
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
        model.Form = MapToForm(selectedOrder);
        return model;
    }

    public async Task<WorkOrderPageViewModel> ResetAsync()
    {
        return await GetEmployeeAsync();
    }

    public async Task<WorkOrderPageViewModel> SaveAsync(WorkOrderPageViewModel model)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to manage work orders.";
            return model;
        }

        if (!current.IsEmployee && !current.IsAdministrator)
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

        if (!TryParseDecimal(model.Form.EstimateAmount, out var estimateAmount))
        {
            model.ErrorMessage = "Estimate amount must be a valid number.";
            return model;
        }

        if (!TryParseDecimal(model.Form.InvoiceAmount, out var invoiceAmount))
        {
            model.ErrorMessage = "Invoice amount must be a valid number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await db.ServiceHistoryEntries.FirstOrDefaultAsync(x => x.Id == model.EditingWorkOrderId.Value);

        if (workOrder is null)
        {
            model.ErrorMessage = "Work order not found.";
            return model;
        }

        workOrder.WorkOrderNumber = Normalize(model.Form.WorkOrderNumber);
        workOrder.VehicleName = Normalize(model.Form.VehicleName) ?? workOrder.VehicleName;
        workOrder.ServiceDate = DateOnly.FromDateTime(serviceDate);
        workOrder.Service = Normalize(model.Form.Service) ?? workOrder.Service;
        workOrder.Complaint = Normalize(model.Form.Complaint);
        workOrder.Diagnosis = Normalize(model.Form.Diagnosis);
        workOrder.Technician = Normalize(model.Form.Technician);
        workOrder.Status = Normalize(model.Form.Status) ?? workOrder.Status;
        workOrder.ApprovalStatus = Normalize(model.Form.ApprovalStatus);
        workOrder.Mileage = mileage;
        workOrder.EstimateAmount = estimateAmount;
        workOrder.InvoiceNumber = Normalize(model.Form.InvoiceNumber);
        workOrder.InvoiceAmount = invoiceAmount;
        workOrder.Notes = Normalize(model.Form.Notes);
        workOrder.InternalNotes = Normalize(model.Form.InternalNotes);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        var refreshed = await GetEmployeeAsync();
        refreshed.SuccessMessage = "Work order updated.";
        return refreshed;
    }

    public async Task<WorkOrderPageViewModel> ApproveAppointmentAsync(Guid requestId)
    {
        return await UpdateAppointmentStatusAsync(
            requestId,
            "Approved",
            "Appointment request approved.");
    }

    public async Task<WorkOrderPageViewModel> DeclineAppointmentAsync(Guid requestId)
    {
        return await UpdateAppointmentStatusAsync(
            requestId,
            "Declined",
            "Appointment request declined.");
    }

    private async Task<WorkOrderPageViewModel> UpdateAppointmentStatusAsync(
        Guid requestId,
        string newStatus,
        string successMessage)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You must be signed in to manage appointment requests."
            };
        }

        if (!current.IsEmployee && !current.IsAdministrator)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You are not authorized to manage appointment requests."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var request = await db.AppointmentRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId);

        if (request is null)
        {
            var model = await GetEmployeeAsync();
            model.ErrorMessage = "Appointment request not found.";
            return model;
        }

        request.Status = newStatus;
        await db.SaveChangesAsync();

        var refreshed = await GetEmployeeAsync();
        refreshed.SuccessMessage = successMessage;
        return refreshed;
    }

    private static WorkOrderEditViewModel MapToForm(ServiceHistoryEntry entry)
    {
        return new WorkOrderEditViewModel
        {
            Id = entry.Id,
            WorkOrderNumber = entry.WorkOrderNumber ?? string.Empty,
            ServiceDate = entry.ServiceDate.ToDateTime(TimeOnly.MinValue),
            VehicleName = entry.VehicleName,
            Service = entry.Service,
            Complaint = entry.Complaint,
            Diagnosis = entry.Diagnosis,
            Technician = entry.Technician,
            Status = entry.Status,
            ApprovalStatus = entry.ApprovalStatus,
            Mileage = entry.Mileage?.ToString(CultureInfo.InvariantCulture),
            EstimateAmount = entry.EstimateAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            InvoiceNumber = entry.InvoiceNumber,
            InvoiceAmount = entry.InvoiceAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            Notes = entry.Notes,
            InternalNotes = entry.InternalNotes
        };
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