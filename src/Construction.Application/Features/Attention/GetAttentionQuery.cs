using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
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
public record GetAttentionQuery : IRequest<AttentionDto>
{
    /// <summary>
    /// Narrows what is counted to one business unit (poslovna jedinica), the same way the lists the kinds link to are
    /// narrowed. A kind that belongs to the company as a whole and to no unit (the virus scanner, the records to tidy)
    /// is not narrowed.
    /// </summary>
    public Guid? BranchId { get; init; }

    /// <summary>With a unit: whose hours count for it, those worked on its sites or those of the people it employs.</summary>
    public BranchBasis Basis { get; init; }
}

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
    private readonly IVirusScanner _scanner;

    public GetAttentionQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ISender sender,
        IVirusScanner scanner)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _sender = sender;
        _scanner = scanner;
    }

    public async Task<AttentionDto> Handle(GetAttentionQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUser.Role;
        var today = _clock.Today;
        var branchId = request.BranchId;
        var token = branchId is { } scoped ? BranchTree.Token(scoped) : string.Empty;
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
            // First, because every file somebody tries to send is refused while the scanner does not answer, and
            // nothing else on this list stops people working like that. A killed scanner leaves its container
            // looking fine, so this is the only place it shows.
            if (_scanner.IsEnabled && !await _scanner.IsReachableAsync(cancellationToken))
            {
                Add("virusScanner", 1);
            }

            var absences = _context.Absences.AsNoTracking().InBranch(branchId).Where(a => a.Status == AbsenceStatus.Requested);
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

            // A cost belongs to the unit its vehicle does.
            if (await BranchScope.VehicleIdsAsync(_context, branchId, cancellationToken) is { } unitVehicles)
            {
                pending = pending.Where(e => unitVehicles.Contains(e.VehicleId));
            }

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
            Add(
                "timeEntriesToReview",
                await _context.TimeEntries.AsNoTracking().InBranch(branchId, request.Basis)
                    .CountAsync(t => t.Status == TimeEntryStatus.Submitted, cancellationToken));

            // Posted from this week on and not yet confirmed on the phone.
            var soon = today.AddDays(7);
            var postings = _context.EmployeeProjects.AsNoTracking()
                .Where(ep => ep.AcknowledgedAt == null
                    && ep.StartDate <= soon
                    && (ep.EndDate == null || ep.EndDate >= today)
                    && ep.Employee.Status == EmployeeStatus.Active);

            if (branchId is not null)
            {
                // A posting is the site's, or, counting people by their employer, the unit that employs them now.
                postings = request.Basis == BranchBasis.Employer
                    ? postings.Where(ep => ep.Employee.BranchPeriods.Any(p => p.Branch.Path.Contains(token) && p.EndDate == null))
                    : postings.Where(ep => ep.Project.Branch!.Path.Contains(token));
            }

            Add("unconfirmedPostings", await postings.CountAsync(cancellationToken));
        }

        if (admin)
        {
            // Orders are the site's, as in the list they link to.
            var orders = _context.ArticleOrders.AsNoTracking().Where(o => o.Status == ArticleOrderStatus.Requested);
            if (branchId is not null)
            {
                orders = orders.Where(o => o.Project != null && o.Project.Branch!.Path.Contains(token));
            }

            Add("articleOrders", await orders.CountAsync(cancellationToken));

            // A refund is the site's when it names one, and otherwise the unit that employs the person.
            var refunds = _context.Refunds.AsNoTracking().Where(r => r.Status == RefundStatus.Requested);
            if (branchId is not null)
            {
                refunds = refunds.Where(r => r.Project != null
                    ? r.Project.Branch!.Path.Contains(token)
                    : r.Employee.BranchPeriods.Any(p => p.Branch.Path.Contains(token) && p.EndDate == null));
            }

            Add("refunds", await refunds.CountAsync(cancellationToken));

            // Fuel rows that need a look. A row that matched no vehicle (an unknown card) is nobody's unit, so it
            // is counted only with the whole company.
            var fuelRows = _context.FuelTransactions.AsNoTracking()
                .Where(t => t.Status == FuelTransactionStatus.NeedsReview
                    || t.Status == FuelTransactionStatus.NoDriverEntry
                    || t.Status == FuelTransactionStatus.UnknownCard);
            if (await BranchScope.VehicleIdsAsync(_context, branchId, cancellationToken) is { } fuelVehicles)
            {
                fuelRows = fuelRows.Where(t => t.VehicleId != null && fuelVehicles.Contains(t.VehicleId.Value));
            }

            Add("dkvRows", await fuelRows.CountAsync(cancellationToken));

            var horizon = today.AddDays(VehicleDateDays);
            var vehicles = await _context.Vehicles.AsNoTracking()
                .InBranch(branchId)
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

            var fines = await _context.VehicleFines.AsNoTracking()
                .Where(f => f.Status == VehicleFineStatus.Open && f.DueDate != null && f.DueDate <= today.AddDays(7))
                .Where(f => branchId == null || f.Vehicle.Branch!.Path.Contains(token)
                    || (f.Vehicle.BranchId == null && f.Vehicle.AssignedProject != null && f.Vehicle.AssignedProject.Branch!.Path.Contains(token)))
                .OrderBy(f => f.DueDate)
                .Select(f => new
                {
                    Name = f.Vehicle.RegistrationNumber + " - " + f.Description,
                    f.DueDate,
                })
                .ToListAsync(cancellationToken);

            Add("vehicleFines", fines.Count, fines.Select(f => new AttentionItemDto { Label = f.Name, Date = f.DueDate }));

            var certificates = _context.EmployeeCertificates.AsNoTracking()
                .Where(c => c.ValidUntil != null && c.ValidUntil <= horizon && c.Employee.Status == EmployeeStatus.Active);
            if (branchId is not null)
            {
                certificates = certificates.Where(c => c.Employee.BranchPeriods.Any(p => p.Branch.Path.Contains(token) && p.EndDate == null));
            }

            var expiring = await certificates
                .OrderBy(c => c.ValidUntil)
                .Select(c => new { Name = c.Employee.FirstName + " " + c.Employee.LastName + " - " + c.Name, c.ValidUntil })
                .ToListAsync(cancellationToken);

            Add("certificatesExpiring", expiring.Count, expiring.Select(c => new AttentionItemDto { Label = c.Name, Date = c.ValidUntil }));

            // Money that is late. Only for those who may open the book of money, since a count of late
            // invoices says something about what the firm is owed.
            if (await Book.BookRules.HasAccessAsync(_context, _currentUser, cancellationToken))
            {
                var lateInvoicesQuery = _context.Invoices.AsNoTracking();
                if (branchId is not null)
                {
                    lateInvoicesQuery = lateInvoicesQuery.Where(i => i.Project.Branch!.Path.Contains(token));
                }

                var late = await lateInvoicesQuery
                    .Where(i => i.Status == InvoiceStatus.Issued && i.Amount > 0 && i.DueDate != null && i.DueDate < today)
                    .Where(i => i.Amount > (i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m))
                    .OrderBy(i => i.DueDate)
                    .Select(i => new { i.Number, Client = i.Project.Customer != null ? i.Project.Customer.Name : i.Project.Name, i.DueDate })
                    .ToListAsync(cancellationToken);

                Add("lateInvoices", late.Count, late.Select(i => new AttentionItemDto { Label = $"{i.Number} - {i.Client}", Date = i.DueDate }));

                var owed = await _context.BookEntries.AsNoTracking()
                    .InBranch(branchId)
                    .Where(e => e.Kind == BookEntryKind.Cost && e.PaidOn == null && e.DueDate != null && e.DueDate < today)
                    .OrderBy(e => e.DueDate)
                    .Select(e => new { Name = e.Counterparty ?? e.Description ?? "", e.DueDate })
                    .ToListAsync(cancellationToken);

                Add("lateCosts", owed.Count, owed.Select(e => new AttentionItemDto { Label = e.Name, Date = e.DueDate }));
            }

            var quality = await _sender.Send(new GetDataQualityQuery(), cancellationToken);
            Add("dataQuality", quality.Groups.Sum(g => g.Count));
        }

        return new AttentionDto { Groups = groups };
    }
}
