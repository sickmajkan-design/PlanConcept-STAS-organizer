using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Projects.Realization.Models;

public class ProjectRevenueDto
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>How a <see cref="ProjectRevenue"/> becomes a <see cref="ProjectRevenueDto"/>.</summary>
public static class ProjectRevenueMapping
{
    public static readonly Expression<Func<ProjectRevenue, ProjectRevenueDto>> Projection =
        revenue => new ProjectRevenueDto
        {
            Id = revenue.Id,
            ProjectId = revenue.ProjectId,
            ProjectName = revenue.Project.Name,
            Amount = revenue.Amount,
            OccurredOn = revenue.OccurredOn,
            Note = revenue.Note,
            RecordedByName = revenue.RecordedByUser != null ? revenue.RecordedByUser.Email : null,
            CreatedAt = revenue.CreatedAt,
        };

    private static readonly Func<ProjectRevenue, ProjectRevenueDto> Compiled = Projection.Compile();

    public static ProjectRevenueDto ToDto(ProjectRevenue revenue) => Compiled(revenue);
}

/// <summary>One project's line on the annual realization plan.</summary>
public class AnnualRealizationRowDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public string Status { get; init; } = null!;

    public decimal ContractValue { get; init; }

    public decimal RealizedThisYear { get; init; }

    public decimal RealizedToDate { get; init; }

    public decimal Remaining => ContractValue - RealizedToDate;

    /// <summary>Null when there is no contract value to measure against.</summary>
    public double? PercentOfContract { get; init; }
}

/// <summary>
/// Contracted value against what has actually come in, summed across every
/// project, for one calendar year.
/// </summary>
public class AnnualRealizationPlanDto
{
    public int Year { get; init; }

    public IReadOnlyList<AnnualRealizationRowDto> Rows { get; init; } =
        Array.Empty<AnnualRealizationRowDto>();

    public decimal TotalContractValue { get; init; }

    public decimal TotalRealizedThisYear { get; init; }

    public decimal TotalRealizedToDate { get; init; }

    public decimal TotalRemaining => TotalContractValue - TotalRealizedToDate;

    public double? PercentRealized { get; init; }
}
