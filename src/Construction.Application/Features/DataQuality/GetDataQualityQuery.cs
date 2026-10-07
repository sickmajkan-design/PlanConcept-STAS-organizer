using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DataQuality;

/// <summary>
/// Records that are incomplete or contradict themselves in ways that quietly spoil other screens: a
/// worker with no position cannot be planned by skill, a vehicle with no dates never reminds anybody,
/// and somebody still posted to a finished project makes the schedule lie.
/// </summary>
/// <remarks>
/// A list of things to tidy, not a gate: nothing here stops anybody from saving anything. Each group
/// carries its full count and the first few records, enough to start on and a link to go on from.
/// </remarks>
public record GetDataQualityQuery : IRequest<DataQualityDto>;

public class DataQualityDto
{
    public IReadOnlyList<DataQualityGroupDto> Groups { get; init; } = [];
}

public class DataQualityGroupDto
{
    /// <summary>Which check this is; the screen supplies the wording and the link for each.</summary>
    public string Key { get; init; } = null!;

    public int Count { get; init; }

    public IReadOnlyList<DataQualityItemDto> Items { get; init; } = [];
}

public class DataQualityItemDto
{
    public Guid Id { get; init; }

    public string Label { get; init; } = null!;

    public string? Detail { get; init; }
}

public class GetDataQualityQueryHandler : IRequestHandler<GetDataQualityQuery, DataQualityDto>
{
    /// <summary>How many records of a group are listed; the count is always the full one.</summary>
    public const int ItemsPerGroup = 50;

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public GetDataQualityQueryHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<DataQualityDto> Handle(GetDataQualityQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var groups = new List<DataQualityGroupDto>();

        var activeEmployees = _context.Employees.AsNoTracking().Where(e => e.Status == EmployeeStatus.Active);

        groups.Add(await GroupAsync(
            "employeesNoPosition",
            activeEmployees.Where(e => e.Position == null || e.Position.Trim() == ""),
            q => q.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                .Select(e => new DataQualityItemDto { Id = e.Id, Label = e.FirstName + " " + e.LastName, Detail = e.EmployeeNumber }),
            cancellationToken));

        var inUse = _context.Vehicles.AsNoTracking().Where(v => v.Status != VehicleStatus.OutOfService);

        groups.Add(await GroupAsync(
            "vehiclesNoTd",
            inUse.Where(v => v.TdNumber == null || v.TdNumber == ""),
            q => q.OrderBy(v => v.RegistrationNumber)
                .Select(v => new DataQualityItemDto { Id = v.Id, Label = v.Brand + " " + v.Model, Detail = v.RegistrationNumber }),
            cancellationToken));

        groups.Add(await GroupAsync(
            "vehiclesNoDates",
            inUse.Where(v => v.RegistrationValidUntil == null || v.TechnicalInspectionValidUntil == null || v.InsuranceValidUntil == null),
            q => q.OrderBy(v => v.RegistrationNumber)
                .Select(v => new DataQualityItemDto
                {
                    Id = v.Id,
                    Label = v.Brand + " " + v.Model + " (" + v.RegistrationNumber + ")",
                    // Comma-separated names of what is missing; the screen words them.
                    Detail = (v.RegistrationValidUntil == null ? "registration," : "")
                        + (v.TechnicalInspectionValidUntil == null ? "inspection," : "")
                        + (v.InsuranceValidUntil == null ? "insurance," : ""),
                }),
            cancellationToken));

        var openProjects = _context.Projects.AsNoTracking().Where(p =>
            p.Status == ProjectStatus.Planned || p.Status == ProjectStatus.Active || p.Status == ProjectStatus.OnHold);

        groups.Add(await GroupAsync(
            "projectsNoNeeds",
            openProjects.Where(p => !p.StaffingNeeds.Any()),
            q => q.OrderBy(p => p.Name).Select(p => new DataQualityItemDto { Id = p.Id, Label = p.Name }),
            cancellationToken));

        groups.Add(await GroupAsync(
            "projectsNoDates",
            openProjects.Where(p => p.StartDate == null || p.EndDate == null),
            q => q.OrderBy(p => p.Name).Select(p => new DataQualityItemDto { Id = p.Id, Label = p.Name }),
            cancellationToken));

        // Still posted today or later to a site that is finished, cancelled or past its end date.
        var stalePostings = _context.EmployeeProjects.AsNoTracking().Where(ep =>
            ep.Employee.Status == EmployeeStatus.Active
            && (ep.EndDate == null || ep.EndDate >= today)
            && (ep.Project.Status == ProjectStatus.Completed
                || ep.Project.Status == ProjectStatus.Cancelled
                || (ep.Project.EndDate != null && ep.Project.EndDate < today)));

        groups.Add(await GroupAsync(
            "postingsAfterEnd",
            stalePostings,
            q => q.OrderBy(ep => ep.Employee.LastName).ThenBy(ep => ep.Employee.FirstName)
                .Select(ep => new DataQualityItemDto
                {
                    Id = ep.EmployeeId,
                    Label = ep.Employee.FirstName + " " + ep.Employee.LastName,
                    Detail = ep.Project.Name,
                }),
            cancellationToken));

        var unknownCards = _context.FuelTransactions.AsNoTracking().Where(t => t.Status == FuelTransactionStatus.UnknownCard);
        var cardNumbers = await unknownCards.Select(t => t.CardNumber).Distinct().OrderBy(c => c).ToListAsync(cancellationToken);

        groups.Add(new DataQualityGroupDto
        {
            Key = "unknownCards",
            Count = cardNumbers.Count,
            Items = cardNumbers.Take(ItemsPerGroup).Select(c => new DataQualityItemDto { Id = Guid.Empty, Label = c }).ToList(),
        });

        return new DataQualityDto { Groups = groups };
    }

    private static async Task<DataQualityGroupDto> GroupAsync<T>(
        string key,
        IQueryable<T> source,
        Func<IQueryable<T>, IQueryable<DataQualityItemDto>> project,
        CancellationToken cancellationToken)
    {
        // An employee with two stale postings is still one thing to fix, so count the people, not the rows.
        var items = await project(source).ToListAsync(cancellationToken);
        var distinct = items.GroupBy(i => (i.Id, i.Label)).Select(g => g.First()).ToList();

        return new DataQualityGroupDto { Key = key, Count = distinct.Count, Items = distinct.Take(ItemsPerGroup).ToList() };
    }
}
