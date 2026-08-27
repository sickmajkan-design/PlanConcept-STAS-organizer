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
        "employeeName", "hourlyRate", "weekendHourlyRate", "holidayHourlyRate",
        "startDate", "endDate", "setByName", "createdAt"
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
            ("hourlyrate", false) => query.OrderBy(r => r.HourlyRate),
            ("hourlyrate", true) => query.OrderByDescending(r => r.HourlyRate),
            ("weekendhourlyrate", false) => query
                .OrderBy(r => r.WeekendHourlyRate == null).ThenBy(r => r.WeekendHourlyRate),
            ("weekendhourlyrate", true) => query
                .OrderByDescending(r => r.WeekendHourlyRate == null).ThenByDescending(r => r.WeekendHourlyRate),
            ("holidayhourlyrate", false) => query
                .OrderBy(r => r.HolidayHourlyRate == null).ThenBy(r => r.HolidayHourlyRate),
            ("holidayhourlyrate", true) => query
                .OrderByDescending(r => r.HolidayHourlyRate == null).ThenByDescending(r => r.HolidayHourlyRate),
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

        return new EmployeeRateSummaryDto
        {
            Count = count,
            AverageHourlyRate = count > 0 ? await query.AverageAsync(r => r.HourlyRate, cancellationToken) : null
        };
    }
}

public record GetMaterialMovementsQuery : ISortablePagedQuery, IRequest<PagedList<MaterialMovementDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "occurredOn", "materialName", "kind", "quantity", "unitPrice", "projectName",
        "recordedByName", "createdAt"
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
