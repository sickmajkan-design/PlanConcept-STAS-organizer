using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Setup.Queries.GetSetupChecklist;

/// <summary>
/// One thing still missing before the system does its job.
/// </summary>
/// <param name="Key">A stable name the client maps to wording and a place to fix it.</param>
/// <param name="Count">How many records are affected; 1 for a yes/no item.</param>
public record SetupChecklistItem(string Key, int Count);

/// <summary>What is still missing. Empty when everything is in place.</summary>
public record SetupChecklistDto(IReadOnlyList<SetupChecklistItem> Items);

/// <summary>
/// Finds the gaps that make the system quietly not work: workers who cannot
/// clock in because they have no account, workers with nowhere to be assigned,
/// sites with no location to measure against, and so on.
/// </summary>
/// <remarks>
/// Computed from the live data on every call rather than kept as a checklist
/// someone ticks off, so it can never say "done" about something that has since
/// broken — a new worker with no account puts the item straight back.
/// </remarks>
public record GetSetupChecklistQuery : IRequest<SetupChecklistDto>;

public class GetSetupChecklistQueryHandler : IRequestHandler<GetSetupChecklistQuery, SetupChecklistDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIntegrationStatus _integrations;

    public GetSetupChecklistQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IIntegrationStatus integrations)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _integrations = integrations;
    }

    public async Task<SetupChecklistDto> Handle(
        GetSetupChecklistQuery request,
        CancellationToken cancellationToken)
    {
        var items = new List<SetupChecklistItem>();

        var year = _dateTimeProvider.UtcNow.Year;

        var company = await _context.CompanySettings
            .AsNoTracking()
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null || string.IsNullOrWhiteSpace(company.Name))
        {
            items.Add(new SetupChecklistItem("companyProfile", 1));
        }

        var employees = _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active);

        if (!await _context.Employees.AnyAsync(cancellationToken))
        {
            items.Add(new SetupChecklistItem("noEmployees", 1));
        }

        var liveProjects = _context.Projects
            .AsNoTracking()
            .Where(p => p.Status == ProjectStatus.Planned
                || p.Status == ProjectStatus.Active
                || p.Status == ProjectStatus.OnHold);

        if (!await _context.Projects.AnyAsync(cancellationToken))
        {
            items.Add(new SetupChecklistItem("noProjects", 1));
        }

        // Only direct employees: a subcontractor has no phone app to sign in to.
        var withoutAccount = await employees
            .CountAsync(e => e.Type == EmployeeType.Employee && e.User == null, cancellationToken);

        if (withoutAccount > 0)
        {
            items.Add(new SetupChecklistItem("employeesWithoutAccount", withoutAccount));
        }

        var withoutProject = await employees
            .CountAsync(e => !e.ProjectAssignments.Any(), cancellationToken);

        if (withoutProject > 0)
        {
            items.Add(new SetupChecklistItem("employeesWithoutProject", withoutProject));
        }

        var withoutLocation = await liveProjects
            .CountAsync(p => p.Latitude == null || p.Longitude == null, cancellationToken);

        if (withoutLocation > 0)
        {
            items.Add(new SetupChecklistItem("projectsWithoutLocation", withoutLocation));
        }

        // A country a site is in that has no public holidays entered for this
        // year: absences and overtime there would be computed as if every day
        // were an ordinary working day.
        var projectCountries = await liveProjects
            .Where(p => p.CountryCode != null)
            .Select(p => p.CountryCode!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (projectCountries.Count > 0)
        {
            var covered = await _context.PublicHolidays
                .AsNoTracking()
                .Where(h => h.Date.Year == year)
                .Select(h => h.CountryCode)
                .Distinct()
                .ToListAsync(cancellationToken);

            var missing = projectCountries.Count(c => !covered.Contains(c));

            if (missing > 0)
            {
                items.Add(new SetupChecklistItem("holidaysMissing", missing));
            }
        }

        // Whoever runs the server sets these up, and that is a SuperAdmin: an Admin
        // could do nothing about them, so telling one only makes noise.
        if (_currentUserService.Role == UserRole.SuperAdmin)
        {
            if (!_integrations.EmailConfigured)
            {
                items.Add(new SetupChecklistItem("emailNotConfigured", 1));
            }

            if (!_integrations.PushConfigured)
            {
                items.Add(new SetupChecklistItem("pushNotConfigured", 1));
            }
        }

        return new SetupChecklistDto(items);
    }
}
