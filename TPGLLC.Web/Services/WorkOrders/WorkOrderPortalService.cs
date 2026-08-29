using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Appointments;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public sealed class WorkOrderPortalService : IWorkOrderPortalService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailSender _emailSender;
    private readonly AppointmentEmailOptions _emailOptions;
    private readonly ILogger<WorkOrderPortalService> _logger;

    public WorkOrderPortalService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IHttpContextAccessor httpContextAccessor,
        IEmailSender emailSender,
        IOptions<AppointmentEmailOptions> emailOptions,
        ILogger<WorkOrderPortalService> logger)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _httpContextAccessor = httpContextAccessor;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
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
            .Include(x => x.Parts)
            .Include(x => x.Jobs)
            .ThenInclude(x => x.Parts)
            .Include(x => x.Inspections)
            .Include(x => x.Updates)
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        await ApplyAppointmentFieldsAsync(db, workOrders);
        await db.SaveChangesAsync();

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
            .Include(x => x.Parts)
            .Include(x => x.Jobs)
            .ThenInclude(x => x.Parts)
            .Include(x => x.Inspections)
            .Include(x => x.Updates)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        var technicianOptions = await GetTechnicianOptionsAsync(db);
        AddExistingTechnicianAssignments(technicianOptions, workOrders);

        await ApplyAppointmentFieldsAsync(db, workOrders);
        await db.SaveChangesAsync();

        return new WorkOrderPageViewModel
        {
            WorkOrders = workOrders,
            TechnicianOptions = technicianOptions,
            CanEdit = true
        };
    }

    public async Task<WorkOrderPageViewModel> GetTechnicianWorkOrdersAsync()
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You must be signed in to view assigned work orders."
            };
        }

        if (!current.IsTechnician)
        {
            return new WorkOrderPageViewModel
            {
                ErrorMessage = "You are not authorized to view technician work orders."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        await EnsureWorkOrderNumbersAsync(db);

        var workOrders = await GetTechnicianWorkOrderQuery(db, current)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        await ApplyAppointmentFieldsAsync(db, workOrders);
        await db.SaveChangesAsync();

        return new WorkOrderPageViewModel
        {
            WorkOrders = workOrders,
            StatusOptions = BuildTechnicianStatusOptions(workOrders),
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
        ApplyAppointmentStatusToWorkOrder(selectedOrder, appointment?.Status);
        model.Form = MapToForm(selectedOrder, appointment);
        model.Form.Jobs = await db.ServiceHistoryJobs.AsNoTracking()
            .Where(x => x.ServiceHistoryEntryId == selectedOrder.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new WorkOrderJobEditViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Status = x.Status,
                LaborAmount = x.LaborAmount.HasValue ? x.LaborAmount.Value.ToString("0.00", CultureInfo.InvariantCulture) : null,
                IsApproved = x.IsApproved,
                IsCustomerDeclined = x.IsCustomerDeclined,
                IsDeferred = x.IsDeferred
            })
            .ToListAsync();
        model.Form.Inspections = await db.ServiceHistoryInspections.AsNoTracking()
            .Where(x => x.ServiceHistoryEntryId == selectedOrder.Id)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new WorkOrderInspectionEditViewModel
            {
                Id = x.Id,
                Area = x.Area,
                Condition = x.Condition,
                Finding = x.Finding,
                Recommendation = x.Recommendation,
                IsCustomerVisible = x.IsCustomerVisible
            })
            .ToListAsync();
        model.Form.Parts = await db.ServiceHistoryParts.AsNoTracking()
            .Where(x => x.ServiceHistoryEntryId == selectedOrder.Id)
            .OrderBy(x => x.Description)
            .Select(x => new WorkOrderPartEditViewModel
            {
                Id = x.Id,
                ServiceHistoryJobId = x.ServiceHistoryJobId,
                Description = x.Description,
                Quantity = x.Quantity.ToString("0.##"),
                UnitPrice = x.UnitPrice.HasValue ? x.UnitPrice.Value.ToString("0.00") : null,
                IsApplied = x.IsApplied,
                IsApproved = x.IsApproved,
                IsCustomerDeclined = x.IsCustomerDeclined
            }).ToListAsync();
        return model;
    }

    public async Task<WorkOrderPageViewModel> StartTechnicianEditAsync(Guid workOrderId)
    {
        var model = await GetTechnicianWorkOrdersAsync();
        if (!string.IsNullOrWhiteSpace(model.ErrorMessage))
        {
            return model;
        }

        var selectedOrder = model.WorkOrders.FirstOrDefault(x => x.Id == workOrderId);
        if (selectedOrder is null)
        {
            model.ErrorMessage = "That work order is not assigned to you.";
            return model;
        }

        model.EditingWorkOrderId = selectedOrder.Id;
        model.Form = MapToForm(selectedOrder);
        model.Form.Parts = selectedOrder.Parts
            .OrderBy(x => x.Description)
            .Select(x => new WorkOrderPartEditViewModel
            {
                Id = x.Id,
                ServiceHistoryJobId = x.ServiceHistoryJobId,
                Description = x.Description,
                Quantity = x.Quantity.ToString("0.##"),
                UnitPrice = x.UnitPrice.HasValue ? x.UnitPrice.Value.ToString("0.00") : null,
                IsApplied = x.IsApplied,
                IsApproved = x.IsApproved,
                IsCustomerDeclined = x.IsCustomerDeclined
            })
            .ToList();

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

        var previousStatus = workOrder.Status;
        var previousDiagnosis = workOrder.Diagnosis;
        var previousNotes = workOrder.Notes;
        var previousMileageOut = workOrder.MileageOut;

        var existingJobs = await db.ServiceHistoryJobs
            .Where(x => x.ServiceHistoryEntryId == workOrder.Id)
            .ToListAsync();
        var previousJobCount = existingJobs.Count;
        var existingInspections = await db.ServiceHistoryInspections
            .Where(x => x.ServiceHistoryEntryId == workOrder.Id)
            .ToListAsync();
        var previousInspectionCount = existingInspections.Count;
        var existingParts = await db.ServiceHistoryParts
            .Where(x => x.ServiceHistoryEntryId == workOrder.Id)
            .ToListAsync();
        var previousAppliedPartCount = existingParts.Count(x => x.IsApplied);

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
        ApplyAppointmentStatusToWorkOrder(workOrder, appointment?.Status);
        workOrder.Mileage = mileage;
        workOrder.MileageOut = mileageOut;
        workOrder.LaborAmount = laborAmount;
        workOrder.InvoiceNumber = workOrder.WorkOrderNumber;
        workOrder.Notes = Normalize(model.Form.Notes);
        workOrder.InternalNotes = Normalize(model.Form.InternalNotes);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;

        var existingJobsById = existingJobs.ToDictionary(x => x.Id);
        var jobIds = new HashSet<Guid>();
        var jobLaborTotal = 0m;
        var approvedJobLaborTotal = 0m;
        var jobSortOrder = 0;
        foreach (var job in model.Form.Jobs.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            if (!TryParseDecimal(job.LaborAmount, out var jobLaborAmount))
            {
                model.ErrorMessage = $"Labor amount for '{job.Name.Trim()}' must be a valid number.";
                return model;
            }

            ServiceHistoryJob? existingJob = null;
            var isExisting = job.Id != Guid.Empty && existingJobsById.TryGetValue(job.Id, out existingJob);
            var savedJob = isExisting
                ? existingJob!
                : new ServiceHistoryJob { ServiceHistoryEntryId = workOrder.Id };

            savedJob.Name = job.Name.Trim();
            savedJob.Description = Normalize(job.Description);
            savedJob.Status = Normalize(job.Status) ?? "Proposed";
            savedJob.LaborAmount = jobLaborAmount;
            savedJob.IsApproved = string.Equals(workOrder.Status, "Quoted", StringComparison.OrdinalIgnoreCase)
                ? job.IsApproved
                : savedJob.IsApproved;
            savedJob.IsCustomerDeclined = isExisting ? existingJob!.IsCustomerDeclined : job.IsCustomerDeclined;
            savedJob.IsDeferred = job.IsDeferred;
            savedJob.SortOrder = jobSortOrder++;
            savedJob.UpdatedUtc = DateTimeOffset.UtcNow;

            if (savedJob.IsApproved)
            {
                savedJob.IsCustomerDeclined = false;
                savedJob.Status = "Approved";
            }

            if (savedJob.IsCustomerDeclined)
            {
                savedJob.IsApproved = false;
                savedJob.Status = "Declined";
            }

            if (!savedJob.IsDeferred && !savedJob.IsCustomerDeclined && savedJob.LaborAmount.HasValue)
            {
                jobLaborTotal += savedJob.LaborAmount.Value;
            }

            if (savedJob.IsApproved && savedJob.LaborAmount.HasValue)
            {
                approvedJobLaborTotal += savedJob.LaborAmount.Value;
            }

            if (!isExisting)
            {
                db.ServiceHistoryJobs.Add(savedJob);
            }

            jobIds.Add(savedJob.Id);
        }

        var removedJobIds = existingJobs
            .Where(x => !jobIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSet();
        foreach (var part in existingParts.Where(x => x.ServiceHistoryJobId.HasValue
            && removedJobIds.Contains(x.ServiceHistoryJobId.Value)))
        {
            part.ServiceHistoryJobId = null;
        }

        db.ServiceHistoryJobs.RemoveRange(existingJobs.Where(x => removedJobIds.Contains(x.Id)));

        var existingInspectionsById = existingInspections.ToDictionary(x => x.Id);
        var inspectionIds = new HashSet<Guid>();
        foreach (var inspection in model.Form.Inspections.Where(x =>
            !string.IsNullOrWhiteSpace(x.Area) || !string.IsNullOrWhiteSpace(x.Finding)))
        {
            if (string.IsNullOrWhiteSpace(inspection.Area) || string.IsNullOrWhiteSpace(inspection.Finding))
            {
                model.ErrorMessage = "Each inspection must include an area and finding.";
                return model;
            }

            ServiceHistoryInspection? existingInspection = null;
            var isExisting = inspection.Id != Guid.Empty
                && existingInspectionsById.TryGetValue(inspection.Id, out existingInspection);
            var savedInspection = isExisting
                ? existingInspection!
                : new ServiceHistoryInspection { ServiceHistoryEntryId = workOrder.Id };

            savedInspection.Area = inspection.Area.Trim();
            savedInspection.Condition = Normalize(inspection.Condition) ?? "Good";
            savedInspection.Finding = inspection.Finding.Trim();
            savedInspection.Recommendation = Normalize(inspection.Recommendation);
            savedInspection.IsCustomerVisible = inspection.IsCustomerVisible;
            savedInspection.UpdatedUtc = DateTimeOffset.UtcNow;

            if (!isExisting)
            {
                db.ServiceHistoryInspections.Add(savedInspection);
            }

            inspectionIds.Add(savedInspection.Id);
        }

        db.ServiceHistoryInspections.RemoveRange(existingInspections.Where(x => !inspectionIds.Contains(x.Id)));

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
            savedPart.ServiceHistoryJobId = part.ServiceHistoryJobId.HasValue
                && jobIds.Contains(part.ServiceHistoryJobId.Value)
                ? part.ServiceHistoryJobId
                : null;
            savedPart.IsApplied = part.IsApplied;
            savedPart.IsApproved = string.Equals(workOrder.Status, "Quoted", StringComparison.OrdinalIgnoreCase)
                ? part.IsApproved
                : savedPart.IsApproved;
            savedPart.IsCustomerDeclined = existing?.IsCustomerDeclined ?? part.IsCustomerDeclined;
            var partTotal = savedPart.Quantity * (savedPart.UnitPrice ?? 0m);
            if (savedPart.IsApplied) appliedPartsTotal += partTotal;
            if (savedPart.IsApproved) approvedPartsTotal += partTotal;
            if (existing is null) db.ServiceHistoryParts.Add(savedPart);
        }
        var submittedIds = model.Form.Parts.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToHashSet();
        db.ServiceHistoryParts.RemoveRange(existingParts.Where(x => !submittedIds.Contains(x.Id)));

        var laborTotal = laborAmount ?? 0m;
        workOrder.EstimateAmount = appliedPartsTotal + laborTotal + jobLaborTotal;
        workOrder.InvoiceAmount = approvedPartsTotal + laborTotal + approvedJobLaborTotal;

        var progressUpdate = AddProgressUpdateIfChanged(
            db,
            workOrder,
            previousStatus,
            previousDiagnosis,
            previousNotes,
            previousMileageOut,
            previousAppliedPartCount,
            model.Form.Parts.Count(x => x.IsApplied),
            current.DisplayName,
            "Service Advisor",
            previousJobCount,
            jobIds.Count,
            previousInspectionCount,
            inspectionIds.Count);

        await db.SaveChangesAsync();

        if (progressUpdate is not null)
        {
            await SendCustomerProgressEmailAsync(db, workOrder, progressUpdate);
        }

        var refreshed = await GetStaffWorkOrdersAsync();
        refreshed.SuccessMessage = $"Work order {requestedWorkOrderNumber} updated.";
        return refreshed;
    }

    public async Task<WorkOrderPageViewModel> SaveTechnicianAsync(WorkOrderPageViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to update work orders.";
            return model;
        }

        if (!current.IsTechnician)
        {
            model.ErrorMessage = "You are not authorized to update technician work orders.";
            return model;
        }

        if (model.EditingWorkOrderId is null)
        {
            model.ErrorMessage = "Select an assigned work order to update.";
            return model;
        }

        if (!TryParseMileage(model.Form.MileageOut, out var mileageOut))
        {
            model.ErrorMessage = "Mileage out must be a whole number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await GetTechnicianWorkOrderQuery(db, current)
            .FirstOrDefaultAsync(x => x.Id == model.EditingWorkOrderId.Value);

        if (workOrder is null)
        {
            model.ErrorMessage = "That work order is not assigned to you.";
            return model;
        }

        var previousStatus = workOrder.Status;
        var previousDiagnosis = workOrder.Diagnosis;
        var previousNotes = workOrder.Notes;
        var previousMileageOut = workOrder.MileageOut;
        var previousAppliedPartCount = workOrder.Parts.Count(x => x.IsApplied);

        var requestedStatus = Normalize(model.Form.Status);
        if (!string.IsNullOrWhiteSpace(requestedStatus)
            && !IsAllowedTechnicianStatus(requestedStatus)
            && !string.Equals(requestedStatus, workOrder.Status, StringComparison.OrdinalIgnoreCase))
        {
            model.ErrorMessage = "Technicians may only set a work order to Requested, In Progress, or Completed.";
            return model;
        }

        workOrder.Diagnosis = Normalize(model.Form.Diagnosis);
        workOrder.MileageOut = mileageOut;
        workOrder.Status = requestedStatus ?? workOrder.Status;
        workOrder.Notes = Normalize(model.Form.Notes);
        workOrder.InternalNotes = Normalize(model.Form.InternalNotes);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;

        var existingParts = await db.ServiceHistoryParts
            .Where(x => x.ServiceHistoryEntryId == workOrder.Id)
            .ToListAsync();
        var existingById = existingParts.ToDictionary(x => x.Id);

        foreach (var part in model.Form.Parts)
        {
            if (part.Id != Guid.Empty && existingById.TryGetValue(part.Id, out var savedPart))
            {
                savedPart.IsApplied = part.IsApplied;
            }
        }

        workOrder.EstimateAmount = CalculateAppliedTotal(workOrder)
            + (workOrder.LaborAmount ?? 0m);

        var progressUpdate = AddProgressUpdateIfChanged(
            db,
            workOrder,
            previousStatus,
            previousDiagnosis,
            previousNotes,
            previousMileageOut,
            previousAppliedPartCount,
            workOrder.Parts.Count(x => x.IsApplied),
            current.DisplayName,
            "Technician");

        await db.SaveChangesAsync();

        if (progressUpdate is not null)
        {
            await SendCustomerProgressEmailAsync(db, workOrder, progressUpdate);
        }

        var refreshed = await GetTechnicianWorkOrdersAsync();
        refreshed.SuccessMessage = $"Work order {workOrder.WorkOrderNumber ?? "record"} updated.";
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

        if (!IsCustomerApprovalOpen(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not waiting for customer approval.");
        }

        var part = workOrder.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Part not found." };
        }

        part.IsApproved = true;
        part.IsCustomerDeclined = false;
        workOrder.Status = "In Progress";
        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Part approved and work is now in progress.");
    }

    public async Task<WorkOrderPageViewModel> DeclinePartAsync(Guid workOrderId, Guid partId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "You must be signed in to decline work." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await GetCustomerWorkOrderAsync(db, workOrderId, current.UserId);
        if (workOrder is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Work order not found." };
        }

        if (!IsCustomerApprovalOpen(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not open for customer decisions.");
        }

        var part = workOrder.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Part not found." };
        }

        part.IsApproved = false;
        part.IsCustomerDeclined = true;
        if (!HasCustomerWorkRemaining(workOrder))
        {
            workOrder.Status = "Declined";
        }

        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Part marked as not approved.");
    }

    public async Task<WorkOrderPageViewModel> ApproveJobAsync(Guid workOrderId, Guid jobId)
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

        if (!IsCustomerApprovalOpen(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not waiting for customer approval.");
        }

        var job = workOrder.Jobs.FirstOrDefault(x => x.Id == jobId);
        if (job is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Repair job not found." };
        }

        job.IsApproved = true;
        job.IsCustomerDeclined = false;
        job.IsDeferred = false;
        job.Status = "Approved";
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        workOrder.Status = "In Progress";
        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Repair job approved and work is now in progress.");
    }

    public async Task<WorkOrderPageViewModel> DeclineJobAsync(Guid workOrderId, Guid jobId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "You must be signed in to decline work." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await GetCustomerWorkOrderAsync(db, workOrderId, current.UserId);
        if (workOrder is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Work order not found." };
        }

        if (!IsCustomerApprovalOpen(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not open for customer decisions.");
        }

        var job = workOrder.Jobs.FirstOrDefault(x => x.Id == jobId);
        if (job is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Repair job not found." };
        }

        job.IsApproved = false;
        job.IsCustomerDeclined = true;
        job.Status = "Declined";
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        if (!HasCustomerWorkRemaining(workOrder))
        {
            workOrder.Status = "Declined";
        }

        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Repair job marked as not approved.");
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

        if (!IsCustomerApprovalOpen(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not waiting for customer approval.");
        }

        foreach (var part in workOrder.Parts.Where(x => x.IsApplied))
        {
            part.IsApproved = true;
            part.IsCustomerDeclined = false;
        }

        foreach (var job in workOrder.Jobs.Where(x => !x.IsDeferred))
        {
            job.IsApproved = true;
            job.IsCustomerDeclined = false;
            job.Status = "Approved";
            job.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        workOrder.Status = "In Progress";
        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("Work order approved and now in progress.");
    }

    public async Task<WorkOrderPageViewModel> DeclineWorkOrderAsync(Guid workOrderId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "You must be signed in to decline work." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await GetCustomerWorkOrderAsync(db, workOrderId, current.UserId);
        if (workOrder is null)
        {
            return new WorkOrderPageViewModel { ErrorMessage = "Work order not found." };
        }

        if (!IsCustomerApprovalOpen(workOrder.Status))
        {
            return await GetCustomerErrorAsync("This work order is not open for customer decisions.");
        }

        foreach (var part in workOrder.Parts.Where(x => x.IsApplied))
        {
            part.IsApproved = false;
            part.IsCustomerDeclined = true;
        }

        foreach (var job in workOrder.Jobs.Where(x => !x.IsDeferred))
        {
            job.IsApproved = false;
            job.IsCustomerDeclined = true;
            job.Status = "Declined";
            job.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        workOrder.Status = "Declined";
        workOrder.InvoiceAmount = CalculateApprovedTotal(workOrder);
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetCustomerResultAsync("All proposed work was not approved.");
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

    private static IQueryable<ServiceHistoryEntry> GetTechnicianWorkOrderQuery(
        TPGLLCDbContext db,
        CurrentCustomer current)
    {
        var assignmentNames = new[]
            {
                current.DisplayName,
                current.Email
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return db.ServiceHistoryEntries
            .Include(x => x.Parts)
            .Include(x => x.Jobs)
            .ThenInclude(x => x.Parts)
            .Include(x => x.Inspections)
            .Include(x => x.Updates)
            .Where(x => x.Technician != null && assignmentNames.Contains(x.Technician.Trim()));
    }

    private static async Task<List<TechnicianOptionViewModel>> GetTechnicianOptionsAsync(TPGLLCDbContext db)
    {
        var users = await db.Users
            .AsNoTracking()
            .Where(x => x.IsActive && db.UserRoles.Any(userRole =>
                userRole.UserId == x.Id &&
                db.Roles.Any(role => role.Id == userRole.RoleId && role.Name == "Technician")))
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .Select(x => new
            {
                x.DisplayName,
                x.FirstName,
                x.LastName,
                x.Email,
                x.UserName
            })
            .ToListAsync();

        return users
            .Select(x =>
            {
                var displayName = string.IsNullOrWhiteSpace(x.DisplayName)
                    ? string.Join(" ", new[] { x.FirstName, x.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim()
                    : x.DisplayName.Trim();
                var email = string.IsNullOrWhiteSpace(x.Email) ? x.UserName?.Trim() : x.Email.Trim();
                var assignmentValue = string.IsNullOrWhiteSpace(displayName) ? email : displayName;

                return new TechnicianOptionViewModel
                {
                    AssignmentValue = assignmentValue ?? string.Empty,
                    Label = string.IsNullOrWhiteSpace(email) || string.Equals(displayName, email, StringComparison.OrdinalIgnoreCase)
                        ? (displayName ?? string.Empty)
                        : $"{displayName} ({email})"
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.AssignmentValue))
            .GroupBy(x => x.AssignmentValue, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Label)
            .ToList();
    }

    private static void AddExistingTechnicianAssignments(
        List<TechnicianOptionViewModel> options,
        IEnumerable<ServiceHistoryEntry> workOrders)
    {
        foreach (var assignment in workOrders
            .Select(x => x.Technician?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (options.Any(x => string.Equals(x.AssignmentValue, assignment, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            options.Add(new TechnicianOptionViewModel
            {
                AssignmentValue = assignment!,
                Label = $"{assignment} (current assignment)"
            });
        }

        options.Sort((left, right) => string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> BuildTechnicianStatusOptions(IEnumerable<ServiceHistoryEntry> workOrders)
    {
        var statuses = WorkOrderStatusCatalog.TechnicianStatuses.ToList();

        foreach (var status in workOrders
            .Select(x => x.Status)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!statuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                statuses.Insert(0, status);
            }
        }

        return statuses;
    }

    private static bool IsAllowedTechnicianStatus(string status) =>
        WorkOrderStatusCatalog.TechnicianStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

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
            .Include(x => x.Jobs)
            .ThenInclude(x => x.Parts)
            .Include(x => x.Inspections)
            .Include(x => x.Updates)
            .Where(x => x.Id == workOrderId && x.Customer != null && x.Customer.ApplicationUserId == userId)
            .FirstOrDefaultAsync();
    }

    private static bool IsCustomerApprovalOpen(string? status) =>
        string.Equals(status, "Waiting on Customer Approval", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "In Progress", StringComparison.OrdinalIgnoreCase);

    private static bool HasCustomerWorkRemaining(ServiceHistoryEntry workOrder) =>
        workOrder.Parts.Any(x => x.IsApplied && !x.IsCustomerDeclined)
        || workOrder.Jobs.Any(x => !x.IsCustomerDeclined && !x.IsDeferred);

    private static decimal CalculateApprovedTotal(ServiceHistoryEntry workOrder)
    {
        if (string.Equals(workOrder.Status, "Declined", StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        var approvedParts = workOrder.Parts.Where(x => x.IsApproved)
            .Sum(x => x.Quantity * (x.UnitPrice ?? 0m));
        var approvedJobs = workOrder.Jobs.Where(x => x.IsApproved)
            .Sum(x => x.LaborAmount ?? 0m);
        return approvedParts + approvedJobs + (workOrder.LaborAmount ?? 0m);
    }

    private static decimal CalculateAppliedTotal(ServiceHistoryEntry workOrder) =>
        workOrder.Parts.Where(x => x.IsApplied)
            .Sum(x => x.Quantity * (x.UnitPrice ?? 0m));

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
            Status = string.IsNullOrWhiteSpace(entry.Status) ? "Requested" : entry.Status,
            ApprovalStatus = appointment?.Status ?? entry.ApprovalStatus,
            Mileage = entry.Mileage?.ToString(CultureInfo.InvariantCulture),
            MileageOut = entry.MileageOut?.ToString(CultureInfo.InvariantCulture),
            EstimateAmount = entry.EstimateAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            LaborAmount = entry.LaborAmount?.ToString("0.00", CultureInfo.InvariantCulture),
            InvoiceNumber = entry.WorkOrderNumber ?? entry.InvoiceNumber,
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
            ApplyAppointmentStatusToWorkOrder(workOrder, appointment.Status);
        }
    }

    private static void ApplyAppointmentStatusToWorkOrder(ServiceHistoryEntry workOrder, string? appointmentStatus)
    {
        if (string.Equals(appointmentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            workOrder.Status = "Cancelled";
            return;
        }

        if (string.Equals(appointmentStatus, "Declined", StringComparison.OrdinalIgnoreCase))
        {
            workOrder.Status = "Declined";
            return;
        }

        if ((string.Equals(appointmentStatus, "Confirmed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(appointmentStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(workOrder.Status)
                || string.Equals(workOrder.Status, "Open", StringComparison.OrdinalIgnoreCase)
                || string.Equals(workOrder.Status, "New", StringComparison.OrdinalIgnoreCase)
                || string.Equals(workOrder.Status, "Requested", StringComparison.OrdinalIgnoreCase)))
        {
            workOrder.Status = "Requested";
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

    private static ServiceHistoryUpdate? AddProgressUpdateIfChanged(
        TPGLLCDbContext db,
        ServiceHistoryEntry workOrder,
        string? previousStatus,
        string? previousDiagnosis,
        string? previousNotes,
        int? previousMileageOut,
        int previousAppliedPartCount,
        int currentAppliedPartCount,
        string? authorName,
        string fallbackAuthor,
        int? previousJobCount = null,
        int? currentJobCount = null,
        int? previousInspectionCount = null,
        int? currentInspectionCount = null)
    {
        var messages = new List<string>();

        if (!string.Equals(previousStatus, workOrder.Status, StringComparison.OrdinalIgnoreCase))
        {
            messages.Add($"Status changed to {workOrder.Status}.");
        }

        if (!string.Equals(previousDiagnosis, workOrder.Diagnosis, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(workOrder.Diagnosis))
            {
                messages.Add($"Findings: {workOrder.Diagnosis}");
            }
        }

        if (!string.Equals(previousNotes, workOrder.Notes, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(workOrder.Notes))
            {
                messages.Add($"Technician note: {workOrder.Notes}");
            }
        }

        if (previousMileageOut != workOrder.MileageOut && workOrder.MileageOut.HasValue)
        {
            messages.Add($"Mileage out recorded at {workOrder.MileageOut.Value:N0}.");
        }

        if (previousAppliedPartCount != currentAppliedPartCount)
        {
            messages.Add($"Parts applied: {currentAppliedPartCount}.");
        }

        if (previousJobCount.HasValue && currentJobCount.HasValue && previousJobCount != currentJobCount)
        {
            messages.Add($"Repair jobs listed: {currentJobCount}.");
        }

        if (previousInspectionCount.HasValue
            && currentInspectionCount.HasValue
            && previousInspectionCount != currentInspectionCount)
        {
            messages.Add($"Inspection findings recorded: {currentInspectionCount}.");
        }

        if (messages.Count == 0)
        {
            return null;
        }

        var progressUpdate = new ServiceHistoryUpdate
        {
            ServiceHistoryEntryId = workOrder.Id,
            Status = string.IsNullOrWhiteSpace(workOrder.Status) ? "Update" : workOrder.Status,
            Message = string.Join(" ", messages),
            AuthorName = string.IsNullOrWhiteSpace(authorName) ? fallbackAuthor : authorName.Trim(),
            IsCustomerVisible = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        db.ServiceHistoryUpdates.Add(progressUpdate);
        return progressUpdate;
    }

    private async Task SendCustomerProgressEmailAsync(
        TPGLLCDbContext db,
        ServiceHistoryEntry workOrder,
        ServiceHistoryUpdate update)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Where(x => x.Id == workOrder.CustomerId)
            .Select(x => new { x.Email, x.FirstName })
            .FirstOrDefaultAsync();

        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
        {
            return;
        }

        var workOrderNumber = WebUtility.HtmlEncode(workOrder.WorkOrderNumber ?? "Work order");
        var vehicle = WebUtility.HtmlEncode(workOrder.VehicleName);
        var status = WebUtility.HtmlEncode(WorkOrderStatusCatalog.GetDefinition(workOrder.Status).Label);
        var message = WebUtility.HtmlEncode(update.Message);
        var firstName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(customer.FirstName) ? "Customer" : customer.FirstName);
        var shopName = WebUtility.HtmlEncode(_emailOptions.ShopName);

        var body = $"""
            <div style="font-family:Arial,sans-serif;max-width:640px;margin:auto;color:#17233c">
              <h2>{shopName}</h2>
              <h3>Repair update for {workOrderNumber}</h3>
              <p>Hello {firstName},</p>
              <p>Your repair status is now <strong>{status}</strong>.</p>
              <p><strong>Vehicle:</strong> {vehicle}</p>
              <div style="margin:20px 0;padding:14px 16px;background:#fffaf0;border:1px solid #f0d38d;border-radius:8px">
                <strong>Latest update</strong><br />
                {message}
              </div>
              <p>Sign in to your customer portal to view the complete repair timeline.</p>
              <p>{WebUtility.HtmlEncode(_emailOptions.ShopPhone)} · {WebUtility.HtmlEncode(_emailOptions.ShopEmail)}</p>
            </div>
            """;

        try
        {
            await _emailSender.SendEmailAsync(
                customer.Email,
                $"{_emailOptions.ShopName} - Repair Update {workOrder.WorkOrderNumber}",
                body);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Work order {WorkOrderId} was updated, but a customer progress email could not be sent.",
                workOrder.Id);
        }
    }
}
