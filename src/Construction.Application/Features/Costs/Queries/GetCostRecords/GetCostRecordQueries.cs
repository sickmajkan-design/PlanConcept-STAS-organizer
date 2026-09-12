using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetCostRecords;

// The four ledgers behind the reports. Grouped in one file because they are
// the same query several times over — filter by owner and date, page,
// project — and splitting them would spread one shape across several folders.

public record GetEmployeeRatesQuery : ISortablePagedQuery, IRequest<PagedList<EmployeeRateDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "employeeName", "rateType", "hourlyRate", "weekendHourlyRate", "holidayHourlyRate",
        "overtimeHourlyRate", "travelHourlyRate",
        "dailyRate", "startDate", "endDate", "setByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? EmployeeId { get; init; }

    /// <summary>Only the rate in force today.</summary>
    public bool CurrentOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetEmployeeRatesQueryValidator : SortablePagedQueryValidator<GetEmployeeRatesQuery>
{
    public GetEmployeeRatesQueryValidator()
        : base(GetEmployeeRatesQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetEmployeeRatesQueryHandler
    : IRequestHandler<GetEmployeeRatesQuery, PagedList<EmployeeRateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetEmployeeRatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<EmployeeRateDto>> Handle(
        GetEmployeeRatesQuery request,
        CancellationToken cancellationToken)
    {
        // Refused rather than narrowed, unlike the rest of the system: there
        // is no useful subset of "everyone's pay" to hand a foreman.
        if (!CostRules.CanSeeLabourCost(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see pay rates.");
        }

        var query = _context.EmployeeRates.AsNoTracking();

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(r => r.EmployeeId == employeeId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<EmployeeRateDto>.CreateAsync(
            query.Select(EmployeeRateMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<EmployeeRate> ApplySorting(
        IQueryable<EmployeeRate> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<EmployeeRate> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("employeename", false) => query
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName),
            ("employeename", true) => query
                .OrderByDescending(r => r.Employee.LastName)
                .ThenByDescending(r => r.Employee.FirstName),
            ("ratetype", false) => query.OrderBy(r => r.RateType),
            ("ratetype", true) => query.OrderByDescending(r => r.RateType),
            ("hourlyrate", false) => query
                .OrderBy(r => r.HourlyRate == null).ThenBy(r => r.HourlyRate),
            ("hourlyrate", true) => query
                .OrderByDescending(r => r.HourlyRate == null).ThenByDescending(r => r.HourlyRate),
            ("weekendhourlyrate", false) => query
                .OrderBy(r => r.WeekendHourlyRate == null).ThenBy(r => r.WeekendHourlyRate),
            ("weekendhourlyrate", true) => query
                .OrderByDescending(r => r.WeekendHourlyRate == null).ThenByDescending(r => r.WeekendHourlyRate),
            ("holidayhourlyrate", false) => query
                .OrderBy(r => r.HolidayHourlyRate == null).ThenBy(r => r.HolidayHourlyRate),
            ("holidayhourlyrate", true) => query
                .OrderByDescending(r => r.HolidayHourlyRate == null).ThenByDescending(r => r.HolidayHourlyRate),
            ("overtimehourlyrate", false) => query
                .OrderBy(r => r.OvertimeHourlyRate == null).ThenBy(r => r.OvertimeHourlyRate),
            ("overtimehourlyrate", true) => query
                .OrderByDescending(r => r.OvertimeHourlyRate == null).ThenByDescending(r => r.OvertimeHourlyRate),
            ("travelhourlyrate", false) => query
                .OrderBy(r => r.TravelHourlyRate == null).ThenBy(r => r.TravelHourlyRate),
            ("travelhourlyrate", true) => query
                .OrderByDescending(r => r.TravelHourlyRate == null).ThenByDescending(r => r.TravelHourlyRate),
            ("dailyrate", false) => query
                .OrderBy(r => r.DailyRate == null).ThenBy(r => r.DailyRate),
            ("dailyrate", true) => query
                .OrderByDescending(r => r.DailyRate == null).ThenByDescending(r => r.DailyRate),
            ("startdate", false) => query.OrderBy(r => r.StartDate),
            ("enddate", false) => query.OrderBy(r => r.EndDate == null).ThenBy(r => r.EndDate),
            ("enddate", true) => query.OrderByDescending(r => r.EndDate == null).ThenByDescending(r => r.EndDate),
            ("setbyname", false) => query
                .OrderBy(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("setbyname", true) => query
                .OrderByDescending(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            // Default and explicit "startDate desc" both land here: the most
            // recently set rate first, which is what "current pay" means.
            _ => query.OrderByDescending(r => r.StartDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(r => r.Id);
    }
}

/// <summary>The count and average of whatever the rates list is currently filtered to.</summary>
public record GetEmployeeRatesSummaryQuery : IRequest<EmployeeRateSummaryDto>
{
    public Guid? EmployeeId { get; init; }

    public bool CurrentOnly { get; init; }
}

public class GetEmployeeRatesSummaryQueryHandler
    : IRequestHandler<GetEmployeeRatesSummaryQuery, EmployeeRateSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetEmployeeRatesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeRateSummaryDto> Handle(
        GetEmployeeRatesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeLabourCost(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see pay rates.");
        }

        var query = _context.EmployeeRates.AsNoTracking();

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(r => r.EmployeeId == employeeId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        var count = await query.CountAsync(cancellationToken);

        // Averaged over hourly rates only — a daily rate has no per-hour
        // figure to blend in, and mixing the two would produce a number
        // that means nothing.
        var hourlyRates = query.Where(r => r.RateType == RateType.Hourly);
        var hourlyCount = await hourlyRates.CountAsync(cancellationToken);

        return new EmployeeRateSummaryDto
        {
            Count = count,
            AverageHourlyRate = hourlyCount > 0
                ? await hourlyRates.AverageAsync(r => r.HourlyRate!.Value, cancellationToken)
                : null
        };
    }
}

public record GetMaterialMovementsQuery : ISortablePagedQuery, IRequest<PagedList<MaterialMovementDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "occurredOn", "materialName", "kind", "quantity", "unitPrice", "projectName",
        "recordedByName", "createdAt", "invoiceNumber"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? MaterialId { get; init; }

    public Guid? ProjectId { get; init; }

    public MaterialMovementKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetMaterialMovementsQueryValidator : SortablePagedQueryValidator<GetMaterialMovementsQuery>
{
    public GetMaterialMovementsQueryValidator()
        : base(GetMaterialMovementsQuery.AllowedSortFields, maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetMaterialMovementsQueryHandler
    : IRequestHandler<GetMaterialMovementsQuery, PagedList<MaterialMovementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMaterialMovementsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<MaterialMovementDto>> Handle(
        GetMaterialMovementsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see stock movements.");
        }

        var query = _context.MaterialMovements.AsNoTracking();

        if (request.MaterialId is { } materialId)
        {
            query = query.Where(m => m.MaterialId == materialId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(m => m.ProjectId == projectId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(m => m.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(m => m.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(m => m.OccurredOn <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<MaterialMovementDto>.CreateAsync(
            query.Select(MaterialMovementMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<MaterialMovement> ApplySorting(
        IQueryable<MaterialMovement> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<MaterialMovement> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("occurredon", false) => query.OrderBy(m => m.OccurredOn),
            ("materialname", false) => query.OrderBy(m => m.Material.Name),
            ("materialname", true) => query.OrderByDescending(m => m.Material.Name),
            ("kind", false) => query.OrderBy(m => m.Kind),
            ("kind", true) => query.OrderByDescending(m => m.Kind),
            ("quantity", false) => query.OrderBy(m => m.Quantity),
            ("quantity", true) => query.OrderByDescending(m => m.Quantity),
            ("unitprice", false) => query.OrderBy(m => m.UnitPrice),
            ("unitprice", true) => query.OrderByDescending(m => m.UnitPrice),
            ("projectname", false) => query
                .OrderBy(m => m.Project != null ? m.Project.Name : null),
            ("projectname", true) => query
                .OrderByDescending(m => m.Project != null ? m.Project.Name : null),
            ("recordedbyname", false) => query
                .OrderBy(m => m.RecordedByUser != null ? m.RecordedByUser.Email : null),
            ("recordedbyname", true) => query
                .OrderByDescending(m => m.RecordedByUser != null ? m.RecordedByUser.Email : null),
            ("createdat", false) => query.OrderBy(m => m.CreatedAt),
            ("createdat", true) => query.OrderByDescending(m => m.CreatedAt),
            ("invoicenumber", false) => query
                .OrderBy(m => m.InvoiceNumber == null).ThenBy(m => m.InvoiceNumber),
            ("invoicenumber", true) => query
                .OrderByDescending(m => m.InvoiceNumber == null).ThenByDescending(m => m.InvoiceNumber),
            // Default and explicit "occurredOn desc" both land here: newest
            // movement first, which is what a running ledger reads as.
            _ => query.OrderByDescending(m => m.OccurredOn)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenByDescending(m => m.CreatedAt).ThenBy(m => m.Id);
    }
}

/// <summary>The count and value of whatever the movements list is currently filtered to.</summary>
public record GetMaterialMovementsSummaryQuery : IRequest<MaterialMovementSummaryDto>
{
    public Guid? MaterialId { get; init; }

    public Guid? ProjectId { get; init; }

    public MaterialMovementKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetMaterialMovementsSummaryQueryHandler
    : IRequestHandler<GetMaterialMovementsSummaryQuery, MaterialMovementSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMaterialMovementsSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MaterialMovementSummaryDto> Handle(
        GetMaterialMovementsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see stock movements.");
        }

        var query = _context.MaterialMovements.AsNoTracking();

        if (request.MaterialId is { } materialId)
        {
            query = query.Where(m => m.MaterialId == materialId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(m => m.ProjectId == projectId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(m => m.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(m => m.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(m => m.OccurredOn <= to);
        }

        var count = await query.CountAsync(cancellationToken);
        var totalCost = await query
            .Where(m => m.UnitPrice != null)
            .SumAsync(m => m.UnitPrice!.Value * (m.Quantity < 0 ? -m.Quantity : m.Quantity), cancellationToken);

        return new MaterialMovementSummaryDto { Count = count, TotalCost = totalCost };
    }
}

public record GetVehicleExpensesQuery : ISortablePagedQuery, IRequest<PagedList<VehicleExpenseDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "occurredOn", "vehicleName", "kind", "amount", "litres", "odometerKm",
        "recordedByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? VehicleId { get; init; }

    public VehicleExpenseKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetVehicleExpensesQueryValidator : SortablePagedQueryValidator<GetVehicleExpensesQuery>
{
    public GetVehicleExpensesQueryValidator()
        : base(GetVehicleExpensesQuery.AllowedSortFields, maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetVehicleExpensesQueryHandler
    : IRequestHandler<GetVehicleExpensesQuery, PagedList<VehicleExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetVehicleExpensesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<VehicleExpenseDto>> Handle(
        GetVehicleExpensesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see vehicle costs.");
        }

        var query = _context.VehicleExpenses.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(e => e.VehicleId == vehicleId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<VehicleExpenseDto>.CreateAsync(
            query.Select(VehicleExpenseMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<VehicleExpense> ApplySorting(
        IQueryable<VehicleExpense> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<VehicleExpense> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("occurredon", false) => query.OrderBy(e => e.OccurredOn),
            ("vehiclename", false) => query
                .OrderBy(e => e.Vehicle.Brand).ThenBy(e => e.Vehicle.Model),
            ("vehiclename", true) => query
                .OrderByDescending(e => e.Vehicle.Brand).ThenByDescending(e => e.Vehicle.Model),
            ("kind", false) => query.OrderBy(e => e.Kind),
            ("kind", true) => query.OrderByDescending(e => e.Kind),
            ("amount", false) => query.OrderBy(e => e.Amount),
            ("amount", true) => query.OrderByDescending(e => e.Amount),
            ("litres", false) => query.OrderBy(e => e.Litres == null).ThenBy(e => e.Litres),
            ("litres", true) => query.OrderByDescending(e => e.Litres == null).ThenByDescending(e => e.Litres),
            ("odometerkm", false) => query.OrderBy(e => e.OdometerKm == null).ThenBy(e => e.OdometerKm),
            ("odometerkm", true) => query.OrderByDescending(e => e.OdometerKm == null).ThenByDescending(e => e.OdometerKm),
            ("recordedbyname", false) => query
                .OrderBy(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("recordedbyname", true) => query
                .OrderByDescending(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("createdat", false) => query.OrderBy(e => e.CreatedAt),
            ("createdat", true) => query.OrderByDescending(e => e.CreatedAt),
            // Default and explicit "occurredOn desc" both land here: newest
            // expense first, which is what a running ledger reads as.
            _ => query.OrderByDescending(e => e.OccurredOn)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenByDescending(e => e.CreatedAt).ThenBy(e => e.Id);
    }
}

/// <summary>The count and total of whatever the vehicle-expense list is currently filtered to.</summary>
public record GetVehicleExpensesSummaryQuery : IRequest<VehicleExpenseSummaryDto>
{
    public Guid? VehicleId { get; init; }

    public VehicleExpenseKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetVehicleExpensesSummaryQueryHandler
    : IRequestHandler<GetVehicleExpensesSummaryQuery, VehicleExpenseSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetVehicleExpensesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VehicleExpenseSummaryDto> Handle(
        GetVehicleExpensesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see vehicle costs.");
        }

        var query = _context.VehicleExpenses.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(e => e.VehicleId == vehicleId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        var count = await query.CountAsync(cancellationToken);
        var totalAmount = await query.SumAsync(e => e.Amount, cancellationToken);
        var totalLitres = await query
            .Where(e => e.Litres != null)
            .SumAsync(e => e.Litres!.Value, cancellationToken);

        return new VehicleExpenseSummaryDto
        {
            Count = count,
            TotalAmount = totalAmount,
            TotalLitres = totalLitres
        };
    }
}

public record GetFinanceEntriesQuery : ISortablePagedQuery, IRequest<PagedList<FinanceEntryDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "employeeName", "kind", "amount", "hoursWorked", "occurredOn", "projectName",
        "recordedByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    public FinanceEntryKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetFinanceEntriesQueryValidator : SortablePagedQueryValidator<GetFinanceEntriesQuery>
{
    public GetFinanceEntriesQueryValidator()
        : base(GetFinanceEntriesQuery.AllowedSortFields, maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetFinanceEntriesQueryHandler
    : IRequestHandler<GetFinanceEntriesQuery, PagedList<FinanceEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFinanceEntriesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<FinanceEntryDto>> Handle(
        GetFinanceEntriesQuery request,
        CancellationToken cancellationToken)
    {
        // Refused rather than narrowed, like pay rates: there is no useful
        // subset of "everyone's pay" to hand a foreman.
        if (!CostRules.CanSeeLabourCost(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see pay entries.");
        }

        var query = _context.FinanceEntries.AsNoTracking();

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectId == projectId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<FinanceEntryDto>.CreateAsync(
            query.Select(FinanceEntryMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<FinanceEntry> ApplySorting(
        IQueryable<FinanceEntry> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<FinanceEntry> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("employeename", false) => query
                .OrderBy(e => e.Employee.LastName).ThenBy(e => e.Employee.FirstName),
            ("employeename", true) => query
                .OrderByDescending(e => e.Employee.LastName).ThenByDescending(e => e.Employee.FirstName),
            ("kind", false) => query.OrderBy(e => e.Kind),
            ("kind", true) => query.OrderByDescending(e => e.Kind),
            ("amount", false) => query.OrderBy(e => e.Amount),
            ("amount", true) => query.OrderByDescending(e => e.Amount),
            ("hoursworked", false) => query.OrderBy(e => e.HoursWorked == null).ThenBy(e => e.HoursWorked),
            ("hoursworked", true) => query.OrderByDescending(e => e.HoursWorked == null).ThenByDescending(e => e.HoursWorked),
            ("occurredon", false) => query.OrderBy(e => e.OccurredOn),
            ("projectname", false) => query
                .OrderBy(e => e.Project != null ? e.Project.Name : null),
            ("projectname", true) => query
                .OrderByDescending(e => e.Project != null ? e.Project.Name : null),
            ("recordedbyname", false) => query
                .OrderBy(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("recordedbyname", true) => query
                .OrderByDescending(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("createdat", false) => query.OrderBy(e => e.CreatedAt),
            ("createdat", true) => query.OrderByDescending(e => e.CreatedAt),
            // Default and explicit "occurredOn desc" both land here: newest
            // entry first, which is what a running ledger reads as.
            _ => query.OrderByDescending(e => e.OccurredOn)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenByDescending(e => e.CreatedAt).ThenBy(e => e.Id);
    }
}

/// <summary>The count and total of whatever the finance-entries list is currently filtered to.</summary>
public record GetFinanceEntriesSummaryQuery : IRequest<FinanceEntrySummaryDto>
{
    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    public FinanceEntryKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetFinanceEntriesSummaryQueryHandler
    : IRequestHandler<GetFinanceEntriesSummaryQuery, FinanceEntrySummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFinanceEntriesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<FinanceEntrySummaryDto> Handle(
        GetFinanceEntriesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeLabourCost(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see pay entries.");
        }

        var query = _context.FinanceEntries.AsNoTracking();

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectId == projectId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        var count = await query.CountAsync(cancellationToken);
        var totalAmount = await query.SumAsync(e => e.Amount, cancellationToken);
        var totalHours = await query
            .Where(e => e.HoursWorked != null)
            .SumAsync(e => e.HoursWorked!.Value, cancellationToken);

        return new FinanceEntrySummaryDto
        {
            Count = count,
            TotalAmount = totalAmount,
            TotalHoursWorked = totalHours
        };
    }
}

public record GetToolExpensesQuery : ISortablePagedQuery, IRequest<PagedList<ToolExpenseDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "occurredOn", "toolName", "kind", "amount", "recordedByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ToolId { get; init; }

    public ToolExpenseKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetToolExpensesQueryValidator : SortablePagedQueryValidator<GetToolExpensesQuery>
{
    public GetToolExpensesQueryValidator()
        : base(GetToolExpensesQuery.AllowedSortFields, maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetToolExpensesQueryHandler
    : IRequestHandler<GetToolExpensesQuery, PagedList<ToolExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetToolExpensesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<ToolExpenseDto>> Handle(
        GetToolExpensesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see tool costs.");
        }

        var query = _context.ToolExpenses.AsNoTracking();

        if (request.ToolId is { } toolId)
        {
            query = query.Where(e => e.ToolId == toolId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ToolExpenseDto>.CreateAsync(
            query.Select(ToolExpenseMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<ToolExpense> ApplySorting(
        IQueryable<ToolExpense> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<ToolExpense> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("occurredon", false) => query.OrderBy(e => e.OccurredOn),
            ("toolname", false) => query.OrderBy(e => e.Tool.Name),
            ("toolname", true) => query.OrderByDescending(e => e.Tool.Name),
            ("kind", false) => query.OrderBy(e => e.Kind),
            ("kind", true) => query.OrderByDescending(e => e.Kind),
            ("amount", false) => query.OrderBy(e => e.Amount),
            ("amount", true) => query.OrderByDescending(e => e.Amount),
            ("recordedbyname", false) => query
                .OrderBy(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("recordedbyname", true) => query
                .OrderByDescending(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("createdat", false) => query.OrderBy(e => e.CreatedAt),
            ("createdat", true) => query.OrderByDescending(e => e.CreatedAt),
            // Default and explicit "occurredOn desc" both land here: newest
            // expense first, which is what a running ledger reads as.
            _ => query.OrderByDescending(e => e.OccurredOn)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenByDescending(e => e.CreatedAt).ThenBy(e => e.Id);
    }
}

/// <summary>The count and total of whatever the tool-expense list is currently filtered to.</summary>
public record GetToolExpensesSummaryQuery : IRequest<ToolExpenseSummaryDto>
{
    public Guid? ToolId { get; init; }

    public ToolExpenseKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetToolExpensesSummaryQueryHandler
    : IRequestHandler<GetToolExpensesSummaryQuery, ToolExpenseSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetToolExpensesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ToolExpenseSummaryDto> Handle(
        GetToolExpensesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see tool costs.");
        }

        var query = _context.ToolExpenses.AsNoTracking();

        if (request.ToolId is { } toolId)
        {
            query = query.Where(e => e.ToolId == toolId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        var count = await query.CountAsync(cancellationToken);
        var totalAmount = await query.SumAsync(e => e.Amount, cancellationToken);

        return new ToolExpenseSummaryDto { Count = count, TotalAmount = totalAmount };
    }
}

public record GetVehicleRentalRatesQuery : ISortablePagedQuery, IRequest<PagedList<VehicleRentalRateDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "vehicleName", "monthlyAmount", "provider", "startDate", "endDate", "setByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? VehicleId { get; init; }

    /// <summary>Only the rate in force today.</summary>
    public bool CurrentOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetVehicleRentalRatesQueryValidator : SortablePagedQueryValidator<GetVehicleRentalRatesQuery>
{
    public GetVehicleRentalRatesQueryValidator()
        : base(GetVehicleRentalRatesQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetVehicleRentalRatesQueryHandler
    : IRequestHandler<GetVehicleRentalRatesQuery, PagedList<VehicleRentalRateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehicleRentalRatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<VehicleRentalRateDto>> Handle(
        GetVehicleRentalRatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see rental rates.");
        }

        var query = _context.VehicleRentalRates.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(r => r.VehicleId == vehicleId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<VehicleRentalRateDto>.CreateAsync(
            query.Select(VehicleRentalRateMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<VehicleRentalRate> ApplySorting(
        IQueryable<VehicleRentalRate> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<VehicleRentalRate> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("vehiclename", false) => query.OrderBy(r => r.Vehicle.Brand).ThenBy(r => r.Vehicle.Model),
            ("vehiclename", true) => query
                .OrderByDescending(r => r.Vehicle.Brand).ThenByDescending(r => r.Vehicle.Model),
            ("monthlyamount", false) => query.OrderBy(r => r.MonthlyAmount),
            ("monthlyamount", true) => query.OrderByDescending(r => r.MonthlyAmount),
            ("provider", false) => query.OrderBy(r => r.Provider == null).ThenBy(r => r.Provider),
            ("provider", true) => query
                .OrderByDescending(r => r.Provider == null).ThenByDescending(r => r.Provider),
            ("enddate", false) => query.OrderBy(r => r.EndDate == null).ThenBy(r => r.EndDate),
            ("enddate", true) => query
                .OrderByDescending(r => r.EndDate == null).ThenByDescending(r => r.EndDate),
            ("setbyname", false) => query
                .OrderBy(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("setbyname", true) => query
                .OrderByDescending(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            // Default and explicit "startDate desc" both land here: the most
            // recently set rate first, which is what "current cost" means.
            _ => query.OrderByDescending(r => r.StartDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(r => r.Id);
    }
}

/// <summary>The count and total monthly commitment of whatever the rental-rates list is currently filtered to.</summary>
public record GetVehicleRentalRatesSummaryQuery : IRequest<VehicleRentalRateSummaryDto>
{
    public Guid? VehicleId { get; init; }

    public bool CurrentOnly { get; init; }
}

public class GetVehicleRentalRatesSummaryQueryHandler
    : IRequestHandler<GetVehicleRentalRatesSummaryQuery, VehicleRentalRateSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehicleRentalRatesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleRentalRateSummaryDto> Handle(
        GetVehicleRentalRatesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see rental rates.");
        }

        var query = _context.VehicleRentalRates.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(r => r.VehicleId == vehicleId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        var count = await query.CountAsync(cancellationToken);
        var totalMonthlyAmount = count > 0
            ? await query.SumAsync(r => r.MonthlyAmount, cancellationToken)
            : 0m;

        return new VehicleRentalRateSummaryDto
        {
            Count = count,
            TotalMonthlyAmount = totalMonthlyAmount
        };
    }
}

public record GetToolRentalRatesQuery : ISortablePagedQuery, IRequest<PagedList<ToolRentalRateDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "toolName", "monthlyAmount", "provider", "startDate", "endDate", "setByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ToolId { get; init; }

    /// <summary>Only the rate in force today.</summary>
    public bool CurrentOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetToolRentalRatesQueryValidator : SortablePagedQueryValidator<GetToolRentalRatesQuery>
{
    public GetToolRentalRatesQueryValidator()
        : base(GetToolRentalRatesQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetToolRentalRatesQueryHandler
    : IRequestHandler<GetToolRentalRatesQuery, PagedList<ToolRentalRateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetToolRentalRatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<ToolRentalRateDto>> Handle(
        GetToolRentalRatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see rental rates.");
        }

        var query = _context.ToolRentalRates.AsNoTracking();

        if (request.ToolId is { } toolId)
        {
            query = query.Where(r => r.ToolId == toolId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ToolRentalRateDto>.CreateAsync(
            query.Select(ToolRentalRateMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<ToolRentalRate> ApplySorting(
        IQueryable<ToolRentalRate> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<ToolRentalRate> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("toolname", false) => query.OrderBy(r => r.Tool.Name),
            ("toolname", true) => query.OrderByDescending(r => r.Tool.Name),
            ("monthlyamount", false) => query.OrderBy(r => r.MonthlyAmount),
            ("monthlyamount", true) => query.OrderByDescending(r => r.MonthlyAmount),
            ("provider", false) => query.OrderBy(r => r.Provider == null).ThenBy(r => r.Provider),
            ("provider", true) => query
                .OrderByDescending(r => r.Provider == null).ThenByDescending(r => r.Provider),
            ("enddate", false) => query.OrderBy(r => r.EndDate == null).ThenBy(r => r.EndDate),
            ("enddate", true) => query
                .OrderByDescending(r => r.EndDate == null).ThenByDescending(r => r.EndDate),
            ("setbyname", false) => query
                .OrderBy(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("setbyname", true) => query
                .OrderByDescending(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            // Default and explicit "startDate desc" both land here: the most
            // recently set rate first, which is what "current cost" means.
            _ => query.OrderByDescending(r => r.StartDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(r => r.Id);
    }
}

/// <summary>The count and total monthly commitment of whatever the rental-rates list is currently filtered to.</summary>
public record GetToolRentalRatesSummaryQuery : IRequest<ToolRentalRateSummaryDto>
{
    public Guid? ToolId { get; init; }

    public bool CurrentOnly { get; init; }
}

public class GetToolRentalRatesSummaryQueryHandler
    : IRequestHandler<GetToolRentalRatesSummaryQuery, ToolRentalRateSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetToolRentalRatesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolRentalRateSummaryDto> Handle(
        GetToolRentalRatesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see rental rates.");
        }

        var query = _context.ToolRentalRates.AsNoTracking();

        if (request.ToolId is { } toolId)
        {
            query = query.Where(r => r.ToolId == toolId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        var count = await query.CountAsync(cancellationToken);
        var totalMonthlyAmount = count > 0
            ? await query.SumAsync(r => r.MonthlyAmount, cancellationToken)
            : 0m;

        return new ToolRentalRateSummaryDto
        {
            Count = count,
            TotalMonthlyAmount = totalMonthlyAmount
        };
    }
}

public record GetGeneralExpensesQuery : ISortablePagedQuery, IRequest<PagedList<GeneralExpenseDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "occurredOn", "category", "amount", "projectName", "employeeName",
        "supplier", "recordedByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public GeneralExpenseCategory? Category { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetGeneralExpensesQueryValidator : SortablePagedQueryValidator<GetGeneralExpensesQuery>
{
    public GetGeneralExpensesQueryValidator()
        : base(GetGeneralExpensesQuery.AllowedSortFields, maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetGeneralExpensesQueryHandler
    : IRequestHandler<GetGeneralExpensesQuery, PagedList<GeneralExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetGeneralExpensesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<GeneralExpenseDto>> Handle(
        GetGeneralExpensesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see costs.");
        }

        var query = _context.GeneralExpenses.AsNoTracking();

        if (request.Category is { } category)
        {
            query = query.Where(e => e.Category == category);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectId == projectId);
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<GeneralExpenseDto>.CreateAsync(
            query.Select(GeneralExpenseMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<GeneralExpense> ApplySorting(
        IQueryable<GeneralExpense> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<GeneralExpense> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("occurredon", false) => query.OrderBy(e => e.OccurredOn),
            ("category", false) => query.OrderBy(e => e.Category),
            ("category", true) => query.OrderByDescending(e => e.Category),
            ("amount", false) => query.OrderBy(e => e.Amount),
            ("amount", true) => query.OrderByDescending(e => e.Amount),
            ("projectname", false) => query
                .OrderBy(e => e.Project != null ? e.Project.Name : null),
            ("projectname", true) => query
                .OrderByDescending(e => e.Project != null ? e.Project.Name : null),
            ("employeename", false) => query
                .OrderBy(e => e.Employee != null ? e.Employee.LastName : null)
                .ThenBy(e => e.Employee != null ? e.Employee.FirstName : null),
            ("employeename", true) => query
                .OrderByDescending(e => e.Employee != null ? e.Employee.LastName : null)
                .ThenByDescending(e => e.Employee != null ? e.Employee.FirstName : null),
            ("supplier", false) => query.OrderBy(e => e.Supplier == null).ThenBy(e => e.Supplier),
            ("supplier", true) => query
                .OrderByDescending(e => e.Supplier == null).ThenByDescending(e => e.Supplier),
            ("recordedbyname", false) => query
                .OrderBy(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("recordedbyname", true) => query
                .OrderByDescending(e => e.RecordedByUser != null ? e.RecordedByUser.Email : null),
            ("createdat", false) => query.OrderBy(e => e.CreatedAt),
            ("createdat", true) => query.OrderByDescending(e => e.CreatedAt),
            // Default and explicit "occurredOn desc" both land here: newest
            // cost first, which is what a running ledger reads as.
            _ => query.OrderByDescending(e => e.OccurredOn)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenByDescending(e => e.CreatedAt).ThenBy(e => e.Id);
    }
}

/// <summary>The count and total of whatever the general-expense list is currently filtered to.</summary>
public record GetGeneralExpensesSummaryQuery : IRequest<GeneralExpenseSummaryDto>
{
    public GeneralExpenseCategory? Category { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetGeneralExpensesSummaryQueryHandler
    : IRequestHandler<GetGeneralExpensesSummaryQuery, GeneralExpenseSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetGeneralExpensesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GeneralExpenseSummaryDto> Handle(
        GetGeneralExpensesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see costs.");
        }

        var query = _context.GeneralExpenses.AsNoTracking();

        if (request.Category is { } category)
        {
            query = query.Where(e => e.Category == category);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectId == projectId);
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        var count = await query.CountAsync(cancellationToken);
        var totalAmount = count > 0
            ? await query.SumAsync(e => e.Amount, cancellationToken)
            : 0m;

        return new GeneralExpenseSummaryDto
        {
            Count = count,
            TotalAmount = totalAmount
        };
    }
}

public record GetAccommodationRatesQuery : ISortablePagedQuery, IRequest<PagedList<AccommodationRateDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "accommodationAddress", "monthlyAmount", "provider", "startDate", "endDate", "setByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? AccommodationId { get; init; }

    /// <summary>Only the rate in force today.</summary>
    public bool CurrentOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetAccommodationRatesQueryValidator : SortablePagedQueryValidator<GetAccommodationRatesQuery>
{
    public GetAccommodationRatesQueryValidator()
        : base(GetAccommodationRatesQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetAccommodationRatesQueryHandler
    : IRequestHandler<GetAccommodationRatesQuery, PagedList<AccommodationRateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationRatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<AccommodationRateDto>> Handle(
        GetAccommodationRatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see accommodation rates.");
        }

        var query = _context.AccommodationRates.AsNoTracking();

        if (request.AccommodationId is { } accommodationId)
        {
            query = query.Where(r => r.AccommodationId == accommodationId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<AccommodationRateDto>.CreateAsync(
            query.Select(AccommodationRateMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<AccommodationRate> ApplySorting(
        IQueryable<AccommodationRate> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<AccommodationRate> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("accommodationaddress", false) => query.OrderBy(r => r.Accommodation.Address),
            ("accommodationaddress", true) => query.OrderByDescending(r => r.Accommodation.Address),
            ("monthlyamount", false) => query.OrderBy(r => r.MonthlyAmount),
            ("monthlyamount", true) => query.OrderByDescending(r => r.MonthlyAmount),
            ("provider", false) => query.OrderBy(r => r.Provider == null).ThenBy(r => r.Provider),
            ("provider", true) => query
                .OrderByDescending(r => r.Provider == null).ThenByDescending(r => r.Provider),
            ("enddate", false) => query.OrderBy(r => r.EndDate == null).ThenBy(r => r.EndDate),
            ("enddate", true) => query
                .OrderByDescending(r => r.EndDate == null).ThenByDescending(r => r.EndDate),
            ("setbyname", false) => query
                .OrderBy(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("setbyname", true) => query
                .OrderByDescending(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.StartDate)
        };

        return ordered.ThenBy(r => r.Id);
    }
}

/// <summary>The count and total monthly commitment of whatever the accommodation-rates list is currently filtered to.</summary>
public record GetAccommodationRatesSummaryQuery : IRequest<AccommodationRateSummaryDto>
{
    public Guid? AccommodationId { get; init; }

    public bool CurrentOnly { get; init; }
}

public class GetAccommodationRatesSummaryQueryHandler
    : IRequestHandler<GetAccommodationRatesSummaryQuery, AccommodationRateSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationRatesSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccommodationRateSummaryDto> Handle(
        GetAccommodationRatesSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see accommodation rates.");
        }

        var query = _context.AccommodationRates.AsNoTracking();

        if (request.AccommodationId is { } accommodationId)
        {
            query = query.Where(r => r.AccommodationId == accommodationId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(r =>
                r.StartDate <= today && (r.EndDate == null || r.EndDate >= today));
        }

        var count = await query.CountAsync(cancellationToken);
        var totalMonthlyAmount = count > 0
            ? await query.SumAsync(r => r.MonthlyAmount, cancellationToken)
            : 0m;

        return new AccommodationRateSummaryDto
        {
            Count = count,
            TotalMonthlyAmount = totalMonthlyAmount
        };
    }
}

public record GetVehicleRentalsOutQuery : ISortablePagedQuery, IRequest<PagedList<VehicleRentalOutDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "renterName", "dailyRate", "startDate", "endDate", "setByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? VehicleId { get; init; }

    /// <summary>Only loans still out (<c>EndDate</c> null).</summary>
    public bool OpenOnly { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetVehicleRentalsOutQueryValidator : SortablePagedQueryValidator<GetVehicleRentalsOutQuery>
{
    public GetVehicleRentalsOutQueryValidator()
        : base(GetVehicleRentalsOutQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetVehicleRentalsOutQueryHandler
    : IRequestHandler<GetVehicleRentalsOutQuery, PagedList<VehicleRentalOutDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetVehicleRentalsOutQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<VehicleRentalOutDto>> Handle(
        GetVehicleRentalsOutQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see vehicle rentals.");
        }

        var query = _context.VehicleRentalsOut.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(r => r.VehicleId == vehicleId);
        }

        if (request.OpenOnly)
        {
            query = query.Where(r => r.EndDate == null);
        }

        if (request.From is { } from)
        {
            query = query.Where(r => r.StartDate >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.StartDate <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<VehicleRentalOutDto>.CreateAsync(
            query.Select(VehicleRentalOutMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<VehicleRentalOut> ApplySorting(
        IQueryable<VehicleRentalOut> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<VehicleRentalOut> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("rentername", false) => query
                .OrderBy(r => r.Customer != null ? r.Customer.Name : r.RenterName),
            ("rentername", true) => query
                .OrderByDescending(r => r.Customer != null ? r.Customer.Name : r.RenterName),
            ("dailyrate", false) => query.OrderBy(r => r.DailyRate),
            ("dailyrate", true) => query.OrderByDescending(r => r.DailyRate),
            ("enddate", false) => query.OrderBy(r => r.EndDate == null).ThenBy(r => r.EndDate),
            ("enddate", true) => query
                .OrderByDescending(r => r.EndDate == null).ThenByDescending(r => r.EndDate),
            ("setbyname", false) => query
                .OrderBy(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("setbyname", true) => query
                .OrderByDescending(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            // Default and explicit "startDate desc" both land here: the most
            // recent loan first.
            _ => query.OrderByDescending(r => r.StartDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(r => r.Id);
    }
}

/// <summary>The count, how many are still out, and the total value of whatever the loans-out list is currently filtered to.</summary>
public record GetVehicleRentalsOutSummaryQuery : IRequest<VehicleRentalOutSummaryDto>
{
    public Guid? VehicleId { get; init; }

    public bool OpenOnly { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetVehicleRentalsOutSummaryQueryHandler
    : IRequestHandler<GetVehicleRentalsOutSummaryQuery, VehicleRentalOutSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehicleRentalsOutSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleRentalOutSummaryDto> Handle(
        GetVehicleRentalsOutSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see vehicle rentals.");
        }

        var query = _context.VehicleRentalsOut.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(r => r.VehicleId == vehicleId);
        }

        if (request.OpenOnly)
        {
            query = query.Where(r => r.EndDate == null);
        }

        if (request.From is { } from)
        {
            query = query.Where(r => r.StartDate >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.StartDate <= to);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var count = await query.CountAsync(cancellationToken);
        var openCount = await query.CountAsync(r => r.EndDate == null, cancellationToken);

        // Days out priced at the day rate: a closed loan by its own span, an
        // open one up to today. +1 because a loan starting and ending the
        // same day is still a day out, not zero.
        var totalValue = count > 0
            ? await query.SumAsync(
                r => r.DailyRate * (
                    (r.EndDate ?? today).DayNumber - r.StartDate.DayNumber + 1),
                cancellationToken)
            : 0m;

        return new VehicleRentalOutSummaryDto
        {
            Count = count,
            OpenCount = openCount,
            TotalValue = totalValue
        };
    }
}

public record GetToolRentalsOutQuery : ISortablePagedQuery, IRequest<PagedList<ToolRentalOutDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "renterName", "dailyRate", "startDate", "endDate", "setByName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ToolId { get; init; }

    /// <summary>Only loans still out (<c>EndDate</c> null).</summary>
    public bool OpenOnly { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetToolRentalsOutQueryValidator : SortablePagedQueryValidator<GetToolRentalsOutQuery>
{
    public GetToolRentalsOutQueryValidator()
        : base(GetToolRentalsOutQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetToolRentalsOutQueryHandler
    : IRequestHandler<GetToolRentalsOutQuery, PagedList<ToolRentalOutDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetToolRentalsOutQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<ToolRentalOutDto>> Handle(
        GetToolRentalsOutQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see tool rentals.");
        }

        var query = _context.ToolRentalsOut.AsNoTracking();

        if (request.ToolId is { } toolId)
        {
            query = query.Where(r => r.ToolId == toolId);
        }

        if (request.OpenOnly)
        {
            query = query.Where(r => r.EndDate == null);
        }

        if (request.From is { } from)
        {
            query = query.Where(r => r.StartDate >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.StartDate <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ToolRentalOutDto>.CreateAsync(
            query.Select(ToolRentalOutMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<ToolRentalOut> ApplySorting(
        IQueryable<ToolRentalOut> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<ToolRentalOut> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("rentername", false) => query
                .OrderBy(r => r.Customer != null ? r.Customer.Name : r.RenterName),
            ("rentername", true) => query
                .OrderByDescending(r => r.Customer != null ? r.Customer.Name : r.RenterName),
            ("dailyrate", false) => query.OrderBy(r => r.DailyRate),
            ("dailyrate", true) => query.OrderByDescending(r => r.DailyRate),
            ("enddate", false) => query.OrderBy(r => r.EndDate == null).ThenBy(r => r.EndDate),
            ("enddate", true) => query
                .OrderByDescending(r => r.EndDate == null).ThenByDescending(r => r.EndDate),
            ("setbyname", false) => query
                .OrderBy(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("setbyname", true) => query
                .OrderByDescending(r => r.SetByUser != null ? r.SetByUser.Email : null),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            // Default and explicit "startDate desc" both land here: the most
            // recent loan first.
            _ => query.OrderByDescending(r => r.StartDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(r => r.Id);
    }
}

/// <summary>The count, how many are still out, and the total value of whatever the loans-out list is currently filtered to.</summary>
public record GetToolRentalsOutSummaryQuery : IRequest<ToolRentalOutSummaryDto>
{
    public Guid? ToolId { get; init; }

    public bool OpenOnly { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetToolRentalsOutSummaryQueryHandler
    : IRequestHandler<GetToolRentalsOutSummaryQuery, ToolRentalOutSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetToolRentalsOutSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolRentalOutSummaryDto> Handle(
        GetToolRentalsOutSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see tool rentals.");
        }

        var query = _context.ToolRentalsOut.AsNoTracking();

        if (request.ToolId is { } toolId)
        {
            query = query.Where(r => r.ToolId == toolId);
        }

        if (request.OpenOnly)
        {
            query = query.Where(r => r.EndDate == null);
        }

        if (request.From is { } from)
        {
            query = query.Where(r => r.StartDate >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.StartDate <= to);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var count = await query.CountAsync(cancellationToken);
        var openCount = await query.CountAsync(r => r.EndDate == null, cancellationToken);

        var totalValue = count > 0
            ? await query.SumAsync(
                r => r.DailyRate * (
                    (r.EndDate ?? today).DayNumber - r.StartDate.DayNumber + 1),
                cancellationToken)
            : 0m;

        return new ToolRentalOutSummaryDto
        {
            Count = count,
            OpenCount = openCount,
            TotalValue = totalValue
        };
    }
}
