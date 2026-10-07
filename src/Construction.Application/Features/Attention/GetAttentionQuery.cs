using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.DataQuality;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attention;

/// <summary>
/// What is waiting for the caller to do something about, across the platform, limited to what their role
/// may act on: requests to answer, costs to review, statements that do not match, dates about to pass.
/// </summary>
/// <remarks>
/// Counts and a few examples per kind, never the work itself: each kind links to the page where it is
/// dealt with. A kind the role cannot act on is left out, so the list is short for everybody and empty for
/// a worker rather than carrying greyed-out rows.
/// </remarks>
public record GetAttentionQuery : IRequest<AttentionDto>;

public class AttentionDto
{
    public IReadOnlyList<AttentionGroupDto> Groups { get; init; } = [];
}

public class AttentionGroupDto
{
    /// <summary>Which kind this is; the screen supplies the wording and the link.</summary>
    public string Key { get; init; } = null!;

    public int Count { get; init; }

    public IReadOnlyList<AttentionItemDto> Items { get; init; } = [];
}

public class AttentionItemDto
{
    public string Label { get; init; } = null!;

    /// <summary>What it is about, where a word is not enough: for a vehicle date, which date.</summary>
    public string? Kind { get; init; }

    public DateOnly? Date { get; init; }
}

public class GetAttentionQueryHandler : IRequestHandler<GetAttentionQuery, AttentionDto>
{
    public const int ExamplesPerGroup = 3;

    /// <summary>How far ahead a vehicle date counts as coming up; the same as the first reminder.</summary>
    public const int VehicleDateDays = 30;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISender _sender;

    public GetAttentionQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _sender = sender;
    }

    public async Task<AttentionDto> Handle(GetAttentionQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUser.Role;
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var admin = role is UserRole.SuperAdmin or UserRole.Admin;
        var manager = admin || role == UserRole.ProjectManager;
        var groups = new List<AttentionGroupDto>();

        void Add(string key, int count, IEnumerable<AttentionItemDto>? items = null)
        {
            if (count > 0)
            {
                groups.Add(new AttentionGroupDto { Key = key, Count = count, Items = (items ?? []).Take(ExamplesPerGroup).ToList() });
            }
        }

        if (admin)
        {
            var absences = _context.Absences.AsNoTracking().Where(a => a.Status == AbsenceStatus.Requested);
            Add(
                "absenceRequests",
                await absences.CountAsync(cancellationToken),
                (await absences.OrderBy(a => a.StartDate).Take(ExamplesPerGroup)
                    .Select(a => new { Name = a.Employee.FirstName + " " + a.Employee.LastName, a.StartDate })
                    .ToListAsync(cancellationToken))
                .Select(a => new AttentionItemDto { Label = a.Name, Date = a.StartDate }));
        }

        if (CostRules.CanReviewSpending(role))
        {
            var pending = _context.VehicleExpenses.AsNoTracking().Where(e => e.Status == VehicleExpenseStatus.Pending);
            Add(
                "expensesToReview",
                await pending.CountAsync(cancellationToken),
                (await pending.OrderBy(e => e.OccurredOn).Take(ExamplesPerGroup)
                    .Select(e => new { Name = e.Vehicle.Brand + " " + e.Vehicle.Model + " (" + e.Vehicle.RegistrationNumber + ")", e.OccurredOn })
                    .ToListAsync(cancellationToken))
                .Select(e => new AttentionItemDto { Label = e.Name, Date = e.OccurredOn }));
        }

        if (manager)
        {
            Add("timeEntriesToReview", await _context.TimeEntries.AsNoTracking().CountAsync(t => t.Status == TimeEntryStatus.Submitted, cancellationToken));

            // Posted from this week on and not yet confirmed on the phone.
            var soon = today.AddDays(7);
            Add(
                "unconfirmedPostings",
                await _context.EmployeeProjects.AsNoTracking().CountAsync(
                    ep => ep.AcknowledgedAt == null
                        && ep.StartDate <= soon
                        && (ep.EndDate == null || ep.EndDate >= today)
                        && ep.Employee.Status == EmployeeStatus.Active,
                    cancellationToken));
        }

        if (admin)
        {
            Add("articleOrders", await _context.ArticleOrders.AsNoTracking().CountAsync(o => o.Status == ArticleOrderStatus.Requested, cancellationToken));
            Add("refunds", await _context.Refunds.AsNoTracking().CountAsync(r => r.Status == RefundStatus.Requested, cancellationToken));

            Add(
                "dkvRows",
                await _context.FuelTransactions.AsNoTracking().CountAsync(
                    t => t.Status == FuelTransactionStatus.NeedsReview
                        || t.Status == FuelTransactionStatus.NoDriverEntry
                        || t.Status == FuelTransactionStatus.UnknownCard,
                    cancellationToken));

            var horizon = today.AddDays(VehicleDateDays);
            var vehicles = await _context.Vehicles.AsNoTracking()
                .Where(v => v.Status != VehicleStatus.OutOfService
                    && ((v.RegistrationValidUntil != null && v.RegistrationValidUntil <= horizon)
                        || (v.TechnicalInspectionValidUntil != null && v.TechnicalInspectionValidUntil <= horizon)
                        || (v.InsuranceValidUntil != null && v.InsuranceValidUntil <= horizon)
                        || (v.NextServiceDue != null && v.NextServiceDue <= horizon)))
                .Select(v => new
                {
                    Name = v.Brand + " " + v.Model + " (" + v.RegistrationNumber + ")",
                    v.RegistrationValidUntil,
                    v.TechnicalInspectionValidUntil,
                    v.InsuranceValidUntil,
                    v.NextServiceDue,
                })
                .ToListAsync(cancellationToken);

            // One line per vehicle: whichever of its dates comes first.
            var soonest = vehicles
                .Select(v =>
                {
                    var first = new (string Kind, DateOnly? Date)[]
                    {
                        ("registration", v.RegistrationValidUntil),
                        ("inspection", v.TechnicalInspectionValidUntil),
                        ("insurance", v.InsuranceValidUntil),
                        ("service", v.NextServiceDue),
                    }.Where(d => d.Date is not null && d.Date <= horizon).OrderBy(d => d.Date).First();

                    return new AttentionItemDto { Label = v.Name, Kind = first.Kind, Date = first.Date };
                })
                .OrderBy(i => i.Date)
                .ToList();

            Add("vehicleDates", soonest.Count, soonest);

            var expiring = await _context.EmployeeCertificates.AsNoTracking()
                .Where(c => c.ValidUntil != null && c.ValidUntil <= horizon && c.Employee.Status == EmployeeStatus.Active)
                .OrderBy(c => c.ValidUntil)
                .Select(c => new { Name = c.Employee.FirstName + " " + c.Employee.LastName + " - " + c.Name, c.ValidUntil })
                .ToListAsync(cancellationToken);

            Add("certificatesExpiring", expiring.Count, expiring.Select(c => new AttentionItemDto { Label = c.Name, Date = c.ValidUntil }));

            var quality = await _sender.Send(new GetDataQualityQuery(), cancellationToken);
            Add("dataQuality", quality.Groups.Sum(g => g.Count));
        }

        return new AttentionDto { Groups = groups };
    }
}
