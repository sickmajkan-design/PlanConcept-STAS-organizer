using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Finance;
using Construction.Application.Features.Costs.Models;
using Construction.Application.Features.Costs.Queries.GetToolCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetProjectCosts;

public class LabourDayDto
{
    public DateOnly Date { get; init; }

    public int Minutes { get; init; }

    public decimal Cost { get; init; }

    public LabourBasis Basis { get; init; }

    /// <summary>The hourly (or daily) rate used; null when no rate covered the day.</summary>
    public decimal? Rate { get; init; }
}

public class LabourLineDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public int Minutes { get; init; }

    public decimal Cost { get; init; }

    /// <summary>Hours no rate covered — reported rather than treated as free.</summary>
    public int UnpricedMinutes { get; init; }

    /// <summary>Day by day, newest first, so it is clear which rate priced which day.</summary>
    public IReadOnlyList<LabourDayDto> Days { get; init; } = [];
}

public class MaterialLineDto
{
    public Guid Id { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string MaterialName { get; init; } = null!;

    public string Unit { get; init; } = null!;

    public decimal Quantity { get; init; }

    /// <summary>Null when the material had no price at issue: it is not in the total.</summary>
    public decimal? UnitPrice { get; init; }

    public decimal? Total { get; init; }

    public string? Note { get; init; }
}

public class GeneralExpenseLineDto
{
    public Guid Id { get; init; }

    public DateOnly OccurredOn { get; init; }

    public GeneralExpenseCategory Category { get; init; }

    public decimal Amount { get; init; }

    public string? EmployeeName { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class AccommodationLineDto
{
    public Guid AccommodationId { get; init; }

    public string AccommodationName { get; init; } = null!;

    public decimal Cost { get; init; }

    public IReadOnlyList<AccommodationPersonLineDto> People { get; init; } = [];
}

public class AccommodationPersonLineDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public int PersonDays { get; init; }

    public decimal Cost { get; init; }
}

/// <summary>A vehicle or tool currently assigned to the site, with what it cost in the period.</summary>
public class AssignedAssetLineDto
{
    /// <summary>"vehicle" or "tool".</summary>
    public string Kind { get; init; } = null!;

    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public decimal Cost { get; init; }
}

public class ManualPayLineDto
{
    public Guid Id { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string EmployeeName { get; init; } = null!;

    public FinanceEntryKind Kind { get; init; }

    public decimal? HoursWorked { get; init; }

    public decimal Amount { get; init; }

    public string? Note { get; init; }
}

/// <summary>Every cost behind one row of the project cost report, itemised.</summary>
public class ProjectCostBreakdownDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public bool IncludesLabour { get; init; }

    /// <summary>The same figures the report shows for this site.</summary>
    public ProjectCostRowDto Summary { get; init; } = null!;

    public IReadOnlyList<LabourLineDto> Labour { get; init; } = [];

    public IReadOnlyList<MaterialLineDto> Materials { get; init; } = [];

    public IReadOnlyList<GeneralExpenseLineDto> GeneralExpenses { get; init; } = [];

    public IReadOnlyList<AccommodationLineDto> Accommodation { get; init; } = [];

    public IReadOnlyList<ManualPayLineDto> ManualPay { get; init; } = [];

    /// <summary>
    /// Vehicles and tools assigned to the site right now. Their cost is the
    /// fleet's own for the period, not attributed to the site: an asset carries
    /// only its current assignment, not the history a fair split would need.
    /// </summary>
    public IReadOnlyList<AssignedAssetLineDto> AssignedAssets { get; init; } = [];
}

public record GetProjectCostBreakdownQuery : IRequest<ProjectCostBreakdownDto>
{
    public Guid ProjectId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}

public class GetProjectCostBreakdownQueryValidator : AbstractValidator<GetProjectCostBreakdownQuery>
{
    public GetProjectCostBreakdownQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The period cannot end before it starts.");
        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber < GetProjectCostsQuery.MaxDays)
            .WithMessage("That is a longer period than can be reported at once.");
    }
}

public class GetProjectCostBreakdownQueryHandler
    : IRequestHandler<GetProjectCostBreakdownQuery, ProjectCostBreakdownDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetProjectCostBreakdownQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<ProjectCostBreakdownDto> Handle(
        GetProjectCostBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;

        if (!CostRules.CanSeeSpending(role))
        {
            throw new ForbiddenAccessException("You may not see cost reports.");
        }

        // Amounts of the company's money sit behind the finance right, on top of the role check.
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var includesLabour = CostRules.CanSeeLabourCost(role);

        // The figures at the top are the report's own, so the two can never disagree.
        var report = await _sender.Send(
            new GetProjectCostsQuery { From = request.From, To = request.To, ProjectId = request.ProjectId },
            cancellationToken);

        var summary = report.Rows.FirstOrDefault(r => r.ProjectId == request.ProjectId)
            ?? new ProjectCostRowDto { ProjectId = request.ProjectId, ProjectName = project.Name };

        var labour = new List<LabourLineDto>();

        if (includesLabour)
        {
            var entries = await ProjectLabourPricing.LoadAsync(
                _context, request.From, request.To, request.ProjectId, cancellationToken);

            var employeeIds = entries.Select(e => e.EmployeeId).Distinct().ToList();

            var names = await _context.Employees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName, cancellationToken);

            labour = entries
                .GroupBy(e => e.EmployeeId)
                .Select(g => new LabourLineDto
                {
                    EmployeeId = g.Key,
                    EmployeeName = names.GetValueOrDefault(g.Key, "?"),
                    Minutes = g.Sum(e => e.Minutes),
                    Cost = decimal.Round(g.Sum(e => e.Cost), 2),
                    UnpricedMinutes = g.Sum(e => e.UnpricedMinutes),
                    Days = g
                        .GroupBy(e => new { e.Day, e.Basis, e.Rate })
                        .Select(d => new LabourDayDto
                        {
                            Date = d.Key.Day,
                            Basis = d.Key.Basis,
                            Rate = d.Key.Rate,
                            Minutes = d.Sum(e => e.Minutes),
                            Cost = decimal.Round(d.Sum(e => e.Cost), 2)
                        })
                        .OrderByDescending(d => d.Date)
                        .ToList()
                })
                .OrderByDescending(l => l.Cost)
                .ThenBy(l => l.EmployeeName)
                .ToList();
        }

        var materials = await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Kind == MaterialMovementKind.Out
                && m.ProjectId == request.ProjectId
                && m.OccurredOn >= request.From
                && m.OccurredOn <= request.To)
            .OrderByDescending(m => m.OccurredOn)
            .Select(m => new MaterialLineDto
            {
                Id = m.Id,
                OccurredOn = m.OccurredOn,
                MaterialName = m.Material.Name,
                Unit = m.Material.Unit,
                Quantity = m.Quantity,
                UnitPrice = m.UnitPrice,
                Total = m.UnitPrice == null ? null : decimal.Round(m.UnitPrice.Value * m.Quantity, 2),
                Note = m.Note
            })
            .ToListAsync(cancellationToken);

        var general = await _context.GeneralExpenses
            .AsNoTracking()
            .Where(e => e.ProjectId == request.ProjectId
                && e.OccurredOn >= request.From
                && e.OccurredOn <= request.To)
            .OrderByDescending(e => e.OccurredOn)
            .Select(e => new GeneralExpenseLineDto
            {
                Id = e.Id,
                OccurredOn = e.OccurredOn,
                Category = e.Category,
                Amount = e.Amount,
                EmployeeName = e.Employee != null ? e.Employee.FirstName + " " + e.Employee.LastName : null,
                Supplier = e.Supplier,
                Note = e.Note
            })
            .ToListAsync(cancellationToken);

        var accommodation = (await ProjectAccommodationCosts.LoadAsync(
                _context, request.From, request.To, request.ProjectId, cancellationToken))
            .Select(s => new AccommodationLineDto
            {
                AccommodationId = s.AccommodationId,
                AccommodationName = s.AccommodationName,
                Cost = s.Cost,
                People = s.People
                    .Select(p => new AccommodationPersonLineDto
                    {
                        EmployeeId = p.EmployeeId,
                        EmployeeName = p.EmployeeName,
                        PersonDays = p.PersonDays,
                        Cost = p.Cost
                    })
                    .ToList()
            })
            .OrderByDescending(a => a.Cost)
            .ToList();

        var manualPay = new List<ManualPayLineDto>();

        if (includesLabour)
        {
            manualPay = await _context.FinanceEntries
                .AsNoTracking()
                .Where(f => f.ProjectId == request.ProjectId
                    && f.OccurredOn >= request.From
                    && f.OccurredOn <= request.To)
                .OrderByDescending(f => f.OccurredOn)
                .Select(f => new ManualPayLineDto
                {
                    Id = f.Id,
                    OccurredOn = f.OccurredOn,
                    EmployeeName = f.Employee.FirstName + " " + f.Employee.LastName,
                    Kind = f.Kind,
                    HoursWorked = f.HoursWorked,
                    Amount = f.Amount,
                    Note = f.Note
                })
                .ToListAsync(cancellationToken);
        }

        var assignedVehicles = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.AssignedProjectId == request.ProjectId)
            .Select(v => new { v.Id, Name = v.Brand + " " + v.Model })
            .ToListAsync(cancellationToken);

        var assignedTools = await _context.Tools
            .AsNoTracking()
            .Where(t => t.AssignedProjectId == request.ProjectId)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        var assets = new List<AssignedAssetLineDto>();

        if (assignedVehicles.Count > 0)
        {
            var fleet = await _sender.Send(
                new GetVehicleCostsQuery { From = request.From, To = request.To }, cancellationToken);
            var costs = fleet.Rows.ToDictionary(r => r.VehicleId, r => r.Total);

            assets.AddRange(assignedVehicles.Select(v => new AssignedAssetLineDto
            {
                Kind = "vehicle", Id = v.Id, Name = v.Name, Cost = costs.GetValueOrDefault(v.Id)
            }));
        }

        if (assignedTools.Count > 0)
        {
            var toolReport = await _sender.Send(
                new GetToolCostsQuery { From = request.From, To = request.To }, cancellationToken);
            var costs = toolReport.Rows.ToDictionary(r => r.ToolId, r => r.Total);

            assets.AddRange(assignedTools.Select(t => new AssignedAssetLineDto
            {
                Kind = "tool", Id = t.Id, Name = t.Name, Cost = costs.GetValueOrDefault(t.Id)
            }));
        }

        return new ProjectCostBreakdownDto
        {
            From = request.From,
            To = request.To,
            ProjectId = project.Id,
            ProjectName = project.Name,
            IncludesLabour = includesLabour,
            Summary = summary,
            Labour = labour,
            Materials = materials,
            GeneralExpenses = general,
            Accommodation = accommodation,
            ManualPay = manualPay,
            AssignedAssets = assets.OrderByDescending(a => a.Cost).ToList()
        };
    }
}
