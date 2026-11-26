using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// Everything the planning screen needs to draw any stretch of days: who is posted where, who is
/// away, and how many people of which position each site needs.
/// </summary>
/// <remarks>
/// Raw facts, not conclusions. Whether a site is short on a given day, and who could stand in, are
/// worked out in the browser from these, so a change made on screen shows at once without another
/// round trip and the same rules serve the screen and the leave-approval dialog.
/// </remarks>
public record GetPlanningQuery : IRequest<PlanningDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetPlanningQueryValidator : AbstractValidator<GetPlanningQuery>
{
    public GetPlanningQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < PlanningLimits.MaxDays)
            .WithMessage($"The planning window can cover at most {PlanningLimits.MaxDays} days.")
            .OverridePropertyName(nameof(GetPlanningQuery.To));
    }
}

public class PlanningDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public DateOnly Today { get; init; }

    public IReadOnlyList<PlanningProjectDto> Projects { get; init; } = [];

    public IReadOnlyList<PlanningEmployeeDto> Employees { get; init; } = [];

    /// <summary>
    /// Vehicles and tools as they are held NOW (who has each, and which site it is on), for the unit in view. It is the
    /// present state, not a history, so for another day the screen shows it as "state today".
    /// </summary>
    public IReadOnlyList<PlanningEquipmentDto> Equipment { get; init; } = [];

    /// <summary>Every position an employee currently holds, to offer when a project's needs are edited.</summary>
    public IReadOnlyList<string> Positions { get; init; } = [];

    /// <summary>Every certificate name in use, workers and projects, to offer when one is entered.</summary>
    public IReadOnlyList<string> CertificateNames { get; init; } = [];

    /// <summary>Public holidays inside the window, with the country they are kept in.</summary>
    public IReadOnlyList<PlanningHolidayDto> Holidays { get; init; } = [];

    /// <summary>Whether the caller may move people. False for a foreman without that right.</summary>
    public bool CanEdit { get; init; }

    /// <summary>Set when the caller sees only one business unit, which this names.</summary>
    public string? ScopeBranchName { get; init; }

    /// <summary>True when the caller is held to one business unit, whether or not it has a name.</summary>
    public bool IsScoped { get; init; }
}

/// <summary>One vehicle or tool, as much as the planning screen needs to draw an icon and say what it is.</summary>
public class PlanningEquipmentDto
{
    public Guid Id { get; init; }

    /// <summary>"Vehicle" or "Tool".</summary>
    public string Kind { get; init; } = null!;

    /// <summary>"Vehicle", "Kit" (a kofer), "BatterySet", "MeasuringSet", "CableSet" or "Other".</summary>
    public string Group { get; init; } = null!;

    /// <summary>Registration and make for a vehicle, the name for a tool.</summary>
    public string Label { get; init; } = null!;

    /// <summary>The vehicle's or tool's own status, so the screen can say when one is in service or broken.</summary>
    public string Status { get; init; } = null!;

    /// <summary>The person who holds it. Null: nobody does.</summary>
    public Guid? EmployeeId { get; init; }

    /// <summary>The site it is on. Null: none.</summary>
    public Guid? ProjectId { get; init; }
}

public class PlanningProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string? CustomerName { get; init; }

    /// <summary>The customer's country (two letters). The site's own is <c>CountryCode</c>.</summary>
    public string? CustomerCountryCode { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public bool WorksSaturdays { get; init; }

    public bool WorksSundays { get; init; }

    /// <summary>Where the site is. Null: no coordinates, so nobody can be said to be on or off it.</summary>
    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    /// <summary>When the shift starts on the site, in company time (the site stores UTC). Null: not entered, so nobody is called late.</summary>
    public TimeOnly? ShiftStartTime { get; set; }

    /// <summary>Whose public holidays the site keeps. Null: none.</summary>
    public string? CountryCode { get; init; }

    public IReadOnlyList<PlanningNeedDto> Needs { get; init; } = [];

    /// <summary>Certificates everybody posted here has to hold.</summary>
    public IReadOnlyList<string> RequiredCertificates { get; init; } = [];
}

public class PlanningHolidayDto
{
    public DateOnly Date { get; init; }

    public string Name { get; init; } = null!;

    public string CountryCode { get; init; } = null!;
}

public class PlanningNeedDto
{
    public string Position { get; init; } = null!;

    public int Count { get; init; }
}

public class PlanningEmployeeDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string Position { get; init; } = null!;

    /// <summary>A subcontractor, shown in purple.</summary>
    public bool IsContractor { get; init; }

    public IReadOnlyList<PlanningPostingDto> Postings { get; init; } = [];

    public IReadOnlyList<PlanningAbsenceDto> Absences { get; init; } = [];

    public IReadOnlyList<PlanningCertificateDto> Certificates { get; init; } = [];

    /// <summary>Where the worker is housed by the company during the window.</summary>
    public IReadOnlyList<PlanningStayDto> Stays { get; init; } = [];
}

public class PlanningStayDto
{
    /// <summary>The accommodation: its name, or its address when it has none.</summary>
    public string Place { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    /// <summary>Null while the stay is open-ended.</summary>
    public DateOnly? EndDate { get; init; }
}

public class PlanningCertificateDto
{
    public string Name { get; init; } = null!;

    /// <summary>The last valid day. Null for one that does not expire.</summary>
    public DateOnly? ValidUntil { get; init; }
}

public class PlanningPostingDto
{
    public Guid ProjectId { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>When the worker confirmed it on their phone. Null while they have not.</summary>
    public DateTime? AcknowledgedAt { get; init; }
}

public class PlanningAbsenceDto
{
    public string Type { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }
}

public class GetPlanningQueryHandler : IRequestHandler<GetPlanningQuery, PlanningDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;

    public GetPlanningQueryHandler(IApplicationDbContext context, IDateTimeProvider clock, ICurrentUserService currentUser)
    {
        _context = context;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PlanningDto> Handle(GetPlanningQuery request, CancellationToken cancellationToken)
    {
        var from = request.From;
        var to = request.To;

        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);

        // A foreman is held to their own unit whatever unit they ask for.
        var branchId = access.IsScoped ? access.BranchId : request.BranchId;

        var employees = await _context.Employees
            .AsNoTracking()
            .InBranch(branchId)
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new
            {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.FirstName,
                e.LastName,
                e.Position,
                IsContractor = e.Type == EmployeeType.Subcontractor,
                Postings = e.ProjectAssignments
                    .Where(a => a.StartDate <= to && (a.EndDate == null || a.EndDate >= from))
                    .Select(a => new PlanningPostingDto { ProjectId = a.ProjectId, StartDate = a.StartDate, EndDate = a.EndDate, AcknowledgedAt = a.AcknowledgedAt })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var ids = employees.Select(e => e.Id).ToList();

        // Approved only: a request is a question, not a fact the plan can lean on.
        var absences = await _context.Absences
            .AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId)
                && a.Status == AbsenceStatus.Approved
                && a.StartDate <= to && a.EndDate >= from)
            .Select(a => new { a.EmployeeId, a.Type, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var postedProjectIds = employees.SelectMany(e => e.Postings).Select(p => p.ProjectId).Distinct().ToList();

        var projects = await _context.Projects
            .AsNoTracking()
            .InBranch(branchId)
            .Where(p => (p.Status == ProjectStatus.Planned || p.Status == ProjectStatus.Active || p.Status == ProjectStatus.OnHold)
                    && (p.EndDate == null || p.EndDate >= from)
                    && (p.StartDate == null || p.StartDate <= to)
                || postedProjectIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => new PlanningProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status.ToString(),
                CustomerName = p.Customer != null ? p.Customer.Name : null,
                CustomerCountryCode = p.Customer != null ? p.Customer.CountryCode : null,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                WorksSaturdays = p.WorksSaturdays,
                WorksSundays = p.WorksSundays,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                ShiftStartTime = p.ShiftStartTime,
                CountryCode = p.CountryCode,
                Needs = p.StaffingNeeds
                    .OrderBy(n => n.Position)
                    .Select(n => new PlanningNeedDto { Position = n.Position, Count = n.Count })
                    .ToList(),
                RequiredCertificates = p.CertificateRequirements.OrderBy(r => r.Name).Select(r => r.Name).ToList(),
            })
            .ToListAsync(cancellationToken);

        // The site stores its shift start in UTC; the screen compares it with the clock on the wall, so it is sent in company time.
        var today = _clock.Today;
        foreach (var p in projects.Where(p => p.ShiftStartTime is not null))
        {
            p.ShiftStartTime = Construction.Application.Common.CompanyTime.LocalTimeOfDay(today.ToDateTime(p.ShiftStartTime!.Value, DateTimeKind.Utc));
        }

        // shortcut: every item in the unit is sent (a few hundred); a company with thousands would need paging or a
        // per-site request.
        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .InBranch(branchId)
            .Where(v => v.Status != VehicleStatus.RentedOut)
            .OrderBy(v => v.RegistrationNumber)
            .Select(v => new PlanningEquipmentDto
            {
                Id = v.Id,
                Kind = "Vehicle",
                Group = "Vehicle",
                Label = v.Brand + " " + v.Model + " · " + v.RegistrationNumber,
                Status = v.Status.ToString(),
                EmployeeId = v.AssignedEmployeeId,
                ProjectId = v.AssignedProjectId,
            })
            .ToListAsync(cancellationToken);

        var tools = await _context.Tools
            .AsNoTracking()
            .InBranch(branchId)
            .Where(t => t.Status != ToolStatus.RentedOut && t.Status != ToolStatus.Retired && t.Status != ToolStatus.Lost)
            .OrderBy(t => t.Name)
            .Select(t => new PlanningEquipmentDto
            {
                Id = t.Id,
                Kind = "Tool",
                Group = t.IsKit ? "Kit" : t.Group.ToString(),
                Label = t.Name,
                Status = t.Status.ToString(),
                EmployeeId = t.AssignedEmployeeId,
                ProjectId = t.AssignedProjectId,
            })
            .ToListAsync(cancellationToken);

        var certificates = await _context.EmployeeCertificates
            .AsNoTracking()
            .Where(c => ids.Contains(c.EmployeeId))
            .Select(c => new { c.EmployeeId, c.Name, c.ValidUntil })
            .ToListAsync(cancellationToken);

        var stays = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => ids.Contains(s.EmployeeId) && s.StartDate <= to && (s.EndDate == null || s.EndDate >= from))
            .Select(s => new { s.EmployeeId, Place = s.Accommodation.Name ?? s.Accommodation.Address, s.StartDate, s.EndDate })
            .ToListAsync(cancellationToken);

        var certificateNames = certificates.Select(c => c.Name)
            .Concat(projects.SelectMany(p => p.RequiredCertificates))
            .GroupBy(PositionKey.Of)
            .Select(g => g.First())
            .OrderBy(n => n)
            .ToList();

        var holidays = await _context.PublicHolidays
            .AsNoTracking()
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .Select(h => new PlanningHolidayDto { Date = h.Date, Name = h.Name, CountryCode = h.CountryCode })
            .ToListAsync(cancellationToken);

        var positions = employees
            .Select(e => e.Position.Trim())
            .Where(p => p.Length > 0)
            .GroupBy(PositionKey.Of)
            .Select(g => g.First())
            .OrderBy(p => p)
            .ToList();

        return new PlanningDto
        {
            From = from,
            To = to,
            Today = _clock.Today,
            Projects = projects,
            Equipment = vehicles.Concat(tools).ToList(),
            Positions = positions,
            CertificateNames = certificateNames,
            Holidays = holidays,
            CanEdit = access.CanEdit,
            IsScoped = access.IsScoped,
            ScopeBranchName = access.BranchName,
            Employees = employees.Select(e => new PlanningEmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Position = e.Position.Trim(),
                IsContractor = e.IsContractor,
                Postings = e.Postings,
                Absences = absences
                    .Where(a => a.EmployeeId == e.Id)
                    .Select(a => new PlanningAbsenceDto { Type = (access.IsScoped && a.Type == AbsenceType.SickLeave ? AbsenceType.Other : a.Type).ToString(), StartDate = a.StartDate, EndDate = a.EndDate })
                    .ToList(),
                Certificates = certificates
                    .Where(c => c.EmployeeId == e.Id)
                    .Select(c => new PlanningCertificateDto { Name = c.Name, ValidUntil = c.ValidUntil })
                    .ToList(),
                Stays = stays
                    .Where(s => s.EmployeeId == e.Id)
                    .OrderBy(s => s.StartDate)
                    .Select(s => new PlanningStayDto { Place = s.Place, StartDate = s.StartDate, EndDate = s.EndDate })
                    .ToList(),
            }).ToList(),
        };
    }
}
