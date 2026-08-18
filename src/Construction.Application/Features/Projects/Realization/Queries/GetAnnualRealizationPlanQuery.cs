using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Projects.Realization.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Realization.Queries;

/// <summary>
/// Contracted value against what has come in, project by project, for one
/// calendar year.
/// </summary>
/// <remarks>
/// "Realized to date" is not bounded by the requested year — a payment from
/// two years ago still counts toward what a contract has earned overall — but
/// "realized this year" is, which is the number a year-end planning meeting
/// actually wants: what did this twelve months bring in, not what has this
/// project ever brought in.
/// </remarks>
public record GetAnnualRealizationPlanQuery : IRequest<AnnualRealizationPlanDto>
{
    public int Year { get; init; }
}

public class GetAnnualRealizationPlanQueryValidator
    : AbstractValidator<GetAnnualRealizationPlanQuery>
{
    public GetAnnualRealizationPlanQueryValidator(IDateTimeProvider dateTimeProvider)
    {
        var thisYear = dateTimeProvider.UtcNow.Year;

        RuleFor(x => x.Year)
            // Wide enough to cover a contract signed years back that is only
            // now being reviewed, narrow enough that a mistyped year is caught.
            .InclusiveBetween(2000, thisYear + 1);
    }
}

public class GetAnnualRealizationPlanQueryHandler
    : IRequestHandler<GetAnnualRealizationPlanQuery, AnnualRealizationPlanDto>
{
    private readonly IApplicationDbContext _context;

    public GetAnnualRealizationPlanQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnnualRealizationPlanDto> Handle(
        GetAnnualRealizationPlanQuery request,
        CancellationToken cancellationToken)
    {
        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        var projects = await _context.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Status,
                p.ContractValue
            })
            .ToListAsync(cancellationToken);

        var revenues = await _context.ProjectRevenues
            .AsNoTracking()
            .Select(r => new { r.ProjectId, r.Amount, r.OccurredOn })
            .ToListAsync(cancellationToken);

        var revenuesByProject = revenues.ToLookup(r => r.ProjectId);

        var rows = projects
            .Select(project =>
            {
                var forProject = revenuesByProject[project.Id];
                var realizedThisYear = forProject
                    .Where(r => r.OccurredOn >= yearStart && r.OccurredOn <= yearEnd)
                    .Sum(r => r.Amount);
                // Up to the end of the requested year, not just the year
                // itself: reviewing 2025 should still count what a contract
                // earned in 2023 and 2024 toward its running total.
                var realizedToDate = forProject
                    .Where(r => r.OccurredOn <= yearEnd)
                    .Sum(r => r.Amount);

                return new AnnualRealizationRowDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    Status = project.Status.ToString(),
                    ContractValue = project.ContractValue ?? 0m,
                    RealizedThisYear = realizedThisYear,
                    RealizedToDate = realizedToDate,
                    PercentOfContract = project.ContractValue is { } contract && contract > 0
                        ? (double)(realizedToDate / contract)
                        : null
                };
            })
            .ToList();

        var totalContract = rows.Sum(r => r.ContractValue);
        var totalRealizedToDate = rows.Sum(r => r.RealizedToDate);

        return new AnnualRealizationPlanDto
        {
            Year = request.Year,
            Rows = rows,
            TotalContractValue = totalContract,
            TotalRealizedThisYear = rows.Sum(r => r.RealizedThisYear),
            TotalRealizedToDate = totalRealizedToDate,
            PercentRealized = totalContract > 0
                ? (double)(totalRealizedToDate / totalContract)
                : null
        };
    }
}
