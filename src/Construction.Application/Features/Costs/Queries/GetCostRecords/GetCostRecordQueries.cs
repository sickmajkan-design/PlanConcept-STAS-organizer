using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetCostRecords;

// The three ledgers behind the reports. Grouped in one file because they are
// the same query three times over — filter by owner and date, page, project —
// and splitting them would spread one shape across three folders.

public record GetEmployeeRatesQuery : IRequest<PagedList<EmployeeRateDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? EmployeeId { get; init; }

    /// <summary>Only the rate in force today.</summary>
    public bool CurrentOnly { get; init; }
}

public class GetEmployeeRatesQueryValidator : AbstractValidator<GetEmployeeRatesQuery>
{
    public GetEmployeeRatesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetEmployeeRatesQueryHandler
    : IRequestHandler<GetEmployeeRatesQuery, PagedList<EmployeeRateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public GetEmployeeRatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
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

        return await PagedList<EmployeeRateDto>.CreateAsync(
            query
                .OrderByDescending(r => r.StartDate)
                .ProjectTo<EmployeeRateDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}

public record GetMaterialMovementsQuery : IRequest<PagedList<MaterialMovementDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? MaterialId { get; init; }

    public Guid? ProjectId { get; init; }

    public MaterialMovementKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetMaterialMovementsQueryValidator : AbstractValidator<GetMaterialMovementsQuery>
{
    public GetMaterialMovementsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

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
    private readonly IMapper _mapper;

    public GetMaterialMovementsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
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

        return await PagedList<MaterialMovementDto>.CreateAsync(
            query
                .OrderByDescending(m => m.OccurredOn)
                .ThenByDescending(m => m.CreatedAt)
                .ProjectTo<MaterialMovementDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}

public record GetVehicleExpensesQuery : IRequest<PagedList<VehicleExpenseDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? VehicleId { get; init; }

    public VehicleExpenseKind? Kind { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetVehicleExpensesQueryValidator : AbstractValidator<GetVehicleExpensesQuery>
{
    public GetVehicleExpensesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

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
    private readonly IMapper _mapper;

    public GetVehicleExpensesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
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

        return await PagedList<VehicleExpenseDto>.CreateAsync(
            query
                .OrderByDescending(e => e.OccurredOn)
                .ThenByDescending(e => e.CreatedAt)
                .ProjectTo<VehicleExpenseDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
