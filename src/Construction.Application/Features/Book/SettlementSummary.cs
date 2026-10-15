using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerChecks;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Book;

public class SettlementSiteDto
{
    public Guid SectionId { get; init; }

    public Guid? ProjectId { get; init; }

    public string Name { get; init; } = null!;

    /// <summary>"Hourly", "FlatRate" or "Measured"; null for a section that is not a project.</summary>
    public string? BillingMode { get; init; }

    public decimal Hours { get; init; }

    /// <summary>What the client is charged per hour, where the site has a price.</summary>
    public decimal? ClientRate { get; init; }

    /// <summary>The invoices issued for the site for this month, not cancelled.</summary>
    public decimal Invoiced { get; init; }

    public decimal Billing { get; init; }

    public decimal Wage { get; init; }

    public decimal Margin => Billing - Wage;

    public decimal Costs { get; init; }

    public decimal Result => Margin - Costs;

    /// <summary>What the month would have billed by the hour, for a site billed by a sum. Null without a price.</summary>
    public decimal? ByHours { get; init; }
}

public class SettlementFixedLineDto
{
    public string Name { get; init; } = null!;

    public decimal Amount { get; init; }
}

public class SettlementSummaryDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public bool HasSettlement { get; init; }

    public Guid? LedgerId { get; init; }

    public DateTime? LockedAt { get; init; }

    public string? UnlockNote { get; init; }

    public IReadOnlyList<SettlementSiteDto> Sites { get; init; } = [];

    public decimal TotalHours => Sites.Sum(s => s.Hours);

    public decimal TotalBilling => Sites.Sum(s => s.Billing);

    public decimal TotalWage => Sites.Sum(s => s.Wage);

    public decimal TotalMargin => Sites.Sum(s => s.Margin);

    public decimal TotalCosts => Sites.Sum(s => s.Costs);

    public decimal TotalResult => Sites.Sum(s => s.Result);

    public IReadOnlyList<SettlementFixedLineDto> FixedLines { get; init; } = [];

    public decimal FixedTotal => FixedLines.Sum(l => l.Amount);

    /// <summary>The result of all sites less the firm's fixed costs.</summary>
    public decimal FirmResult => TotalResult - FixedTotal;

    /// <summary>What the settlement's own list says to look at before the month is closed.</summary>
    public IReadOnlyList<LedgerCheckDto> Checks { get; init; } = [];

    /// <summary>How many costs booked in the month have no document attached. A prompt, never a block.</summary>
    public int CostsWithoutDocumentCount { get; init; }

    /// <summary>Their total, so the size of the gap is visible at a glance.</summary>
    public decimal CostsWithoutDocumentAmount { get; init; }
}

/// <summary>
/// A month's settlement one line per site: how it is billed, hours, billing, what the workers earned,
/// margin, costs and result, then the fixed costs down to what the firm earned.
/// </summary>
public record GetSettlementSummaryQuery : IRequest<SettlementSummaryDto>
{
    public int Year { get; init; }

    public int Month { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetSettlementSummaryQueryValidator : AbstractValidator<GetSettlementSummaryQuery>
{
    public GetSettlementSummaryQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetSettlementSummaryQueryHandler : IRequestHandler<GetSettlementSummaryQuery, SettlementSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetSettlementSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<SettlementSummaryDto> Handle(
        GetSettlementSummaryQuery request, CancellationToken cancellationToken)
    {
        // Like the settlement itself: the Super Admin's.
        if (_currentUserService.Role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Only the Super Admin works with the monthly settlement.");
        }

        var first = new DateOnly(request.Year, request.Month, 1);
        var last = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));

        // What the firm itself costs in the month, whether or not a settlement exists.
        var fixedLines = new List<SettlementFixedLineDto>();

        var entries = _context.BookEntries.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.Kind == BookEntryKind.Cost
                && e.Category.Bucket == BookCategoryBucket.Fixed
                && e.OccurredOn >= first && e.OccurredOn <= last);

        var fixedEntries = await entries
            .OrderBy(e => e.OccurredOn)
            .Select(e => new { Name = e.Description ?? e.Category.Name, e.Amount })
            .ToListAsync(cancellationToken);

        fixedLines.AddRange(fixedEntries.Select(e => new SettlementFixedLineDto { Name = e.Name, Amount = e.Amount }));

        var general = await _context.GeneralExpenses.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.OccurredOn >= first && e.OccurredOn <= last
                && e.Category != GeneralExpenseCategory.Housing)
            .OrderBy(e => e.OccurredOn)
            .Select(e => new { e.Supplier, e.Category, e.Amount })
            .ToListAsync(cancellationToken);

        fixedLines.AddRange(general.Select(e => new SettlementFixedLineDto
        {
            Name = e.Supplier ?? e.Category.ToString(),
            Amount = e.Amount,
        }));

        // Costs the accountant will ask a paper for: every cost entry of the month with nothing filed on it.
        var undocumented = await _context.BookEntries.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.Kind == BookEntryKind.Cost
                && e.OccurredOn >= first && e.OccurredOn <= last
                && !_context.Attachments.Any(a => a.BookEntryId == e.Id))
            .Select(e => e.Amount)
            .ToListAsync(cancellationToken);

        var ledgers = _context.Ledgers.AsNoTracking()
            .Where(l => l.Year == request.Year && l.Month == request.Month);

        if (request.BranchId is { } branchId)
        {
            var token = BranchTree.Token(branchId);
            ledgers = ledgers.Where(l => l.Branch!.Path.Contains(token));
        }

        var ledger = await ledgers
            .OrderBy(l => l.CreatedAt)
            .Select(l => new { l.Id, l.LockedAt, l.UnlockNote, l.ResultJson })
            .FirstOrDefaultAsync(cancellationToken);

        if (ledger is null)
        {
            return new SettlementSummaryDto
            {
                Year = request.Year,
                Month = request.Month,
                FixedLines = fixedLines,
                CostsWithoutDocumentCount = undocumented.Count,
                CostsWithoutDocumentAmount = undocumented.Sum(),
            };
        }

        // A closed month shows the sites as they were when it was closed.
        var stored = ledger.LockedAt is not null ? LedgerMonthResult.Parse(ledger.ResultJson) : null;

        var figures = stored?.Sites
            ?? (await LedgerMonthResults.ComputeAsync(_context, ledger.Id, cancellationToken)).Sites
            ?? [];

        var projectIds = figures.Where(f => f.ProjectId != null).Select(f => f.ProjectId!.Value).Distinct().ToList();

        var projects = await _context.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.BillingMode, p.ClientHourlyRate })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var invoiced = await _context.Invoices.AsNoTracking()
            .Where(i => projectIds.Contains(i.ProjectId)
                && i.Status != InvoiceStatus.Cancelled
                && i.PayrollYear == request.Year && i.PayrollMonth == request.Month)
            .GroupBy(i => i.ProjectId)
            .Select(g => new { ProjectId = g.Key, Amount = g.Sum(i => i.Amount) })
            .ToDictionaryAsync(g => g.ProjectId, g => g.Amount, cancellationToken);

        var sites = figures.Select(f =>
        {
            var project = f.ProjectId is { } id && projects.TryGetValue(id, out var p) ? p : null;
            var rate = project?.ClientHourlyRate;
            var mode = project?.BillingMode;

            return new SettlementSiteDto
            {
                SectionId = f.SectionId,
                ProjectId = f.ProjectId,
                Name = f.Name,
                BillingMode = mode?.ToString(),
                Hours = f.Hours,
                ClientRate = rate,
                Invoiced = f.ProjectId is { } pid ? invoiced.GetValueOrDefault(pid) : 0m,
                Billing = f.Billing,
                Wage = f.Wage,
                Costs = f.Costs,
                ByHours = mode is ProjectBillingMode.FlatRate or ProjectBillingMode.Measured && rate is { } r
                    ? Math.Round(f.Hours * r, 2)
                    : null,
            };
        }).ToList();

        var checks = ledger.LockedAt is null
            ? await _sender.Send(new GetLedgerChecksQuery { LedgerId = ledger.Id }, cancellationToken)
            : [];

        return new SettlementSummaryDto
        {
            Year = request.Year,
            Month = request.Month,
            HasSettlement = true,
            LedgerId = ledger.Id,
            LockedAt = ledger.LockedAt,
            UnlockNote = ledger.UnlockNote,
            Sites = sites,
            FixedLines = fixedLines,
            Checks = checks,
            CostsWithoutDocumentCount = undocumented.Count,
            CostsWithoutDocumentAmount = undocumented.Sum(),
        };
    }
}

/// <summary>
/// How a site is billed and at what price per hour. Changing it moves the site's billing in this and the
/// following settlements; a closed month keeps what it was closed with.
/// </summary>
public record UpdateSiteBillingCommand : IRequest
{
    /// <summary>Taken from the route.</summary>
    public Guid ProjectId { get; init; }

    public ProjectBillingMode BillingMode { get; init; }

    /// <summary>Null clears the price.</summary>
    public decimal? ClientHourlyRate { get; init; }
}

public class UpdateSiteBillingCommandValidator : AbstractValidator<UpdateSiteBillingCommand>
{
    public UpdateSiteBillingCommandValidator()
    {
        RuleFor(x => x.BillingMode).IsInEnum();
        RuleFor(x => x.ClientHourlyRate).InclusiveBetween(0m, 99999m).When(x => x.ClientHourlyRate is not null);
    }
}

public class UpdateSiteBillingCommandHandler : IRequestHandler<UpdateSiteBillingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateSiteBillingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateSiteBillingCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        project.BillingMode = request.BillingMode;
        project.ClientHourlyRate = request.ClientHourlyRate;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
