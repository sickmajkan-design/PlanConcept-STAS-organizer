using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Book;

/// <summary>How late a debt is, in the same four buckets for what clients owe and what the firm owes.</summary>
public static class DebtAge
{
    public const string NotDue = "NotDue";
    public const string Days1To30 = "Days1To30";
    public const string Days31To60 = "Days31To60";
    public const string Over60 = "Over60";

    public static readonly IReadOnlyList<string> All = [NotDue, Days1To30, Days31To60, Over60];

    /// <summary>A debt with no due date, or one not yet due, is "not due".</summary>
    public static (string Bucket, int DaysOverdue) Of(DateOnly? dueDate, DateOnly today)
    {
        if (dueDate is not { } due || due >= today)
        {
            return (NotDue, 0);
        }

        var days = today.DayNumber - due.DayNumber;

        return days switch
        {
            <= 30 => (Days1To30, days),
            <= 60 => (Days31To60, days),
            _ => (Over60, days),
        };
    }
}

public class DebtRowDto
{
    /// <summary>"Invoice", "BookEntry" or "Payroll": where the debt is kept, and so where it is settled.</summary>
    public string Source { get; init; } = null!;

    public Guid Id { get; init; }

    /// <summary>The invoice number, or the description of the entry.</summary>
    public string Title { get; init; } = null!;

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    /// <summary>The client for what is owed to the firm, the supplier for what the firm owes.</summary>
    public string? Counterparty { get; init; }

    public DateOnly IssuedOn { get; init; }

    public DateOnly? DueDate { get; init; }

    public decimal Amount { get; init; }

    public decimal PaidAmount { get; init; }

    public decimal OpenAmount { get; init; }

    public int DaysOverdue { get; init; }

    /// <summary>"NotDue", "Days1To30", "Days31To60" or "Over60".</summary>
    public string Age { get; init; } = DebtAge.NotDue;

    public Guid? BranchId { get; init; }
}

public class DebtBucketDto
{
    public string Age { get; init; } = null!;

    public decimal Amount { get; init; }

    public int Count { get; init; }
}

public class DebtsDto
{
    public DateOnly AsOf { get; init; }

    /// <summary>Everything still open.</summary>
    public decimal OpenTotal { get; init; }

    /// <summary>The part of it whose due date has passed.</summary>
    public decimal OverdueTotal { get; init; }

    /// <summary>All four buckets, in order, whether empty or not.</summary>
    public IReadOnlyList<DebtBucketDto> Buckets { get; init; } = [];

    /// <summary>Most overdue first, then by due date.</summary>
    public IReadOnlyList<DebtRowDto> Rows { get; init; } = [];
}

internal static class DebtBuilder
{
    public static DebtsDto Build(DateOnly today, IEnumerable<DebtRowDto> rows)
    {
        var list = rows
            .Where(r => r.OpenAmount > 0)
            .OrderByDescending(r => r.DaysOverdue)
            .ThenBy(r => r.DueDate ?? DateOnly.MaxValue)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var buckets = DebtAge.All
            .Select(age => new DebtBucketDto
            {
                Age = age,
                Amount = list.Where(r => r.Age == age).Sum(r => r.OpenAmount),
                Count = list.Count(r => r.Age == age),
            })
            .ToList();

        return new DebtsDto
        {
            AsOf = today,
            OpenTotal = list.Sum(r => r.OpenAmount),
            OverdueTotal = list.Where(r => r.Age != DebtAge.NotDue).Sum(r => r.OpenAmount),
            Buckets = buckets,
            Rows = list,
        };
    }
}

/// <summary>
/// What clients still owe: invoices not yet paid in full, less the part-payments, and incomes entered in the
/// book that are not marked paid. Aged by due date.
/// </summary>
public record GetReceivablesQuery : IRequest<DebtsDto>
{
    public Guid? BranchId { get; init; }

    public Guid? ProjectId { get; init; }
}

public class GetReceivablesQueryHandler : IRequestHandler<GetReceivablesQuery, DebtsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetReceivablesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DebtsDto> Handle(GetReceivablesQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var branchId = request.BranchId;

        var invoices = _context.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued && i.Amount > 0);

        if (branchId is { } id)
        {
            var token = BranchTree.Token(id);
            invoices = invoices.Where(i => i.Project.Branch!.Path.Contains(token));
        }

        if (request.ProjectId is { } projectId)
        {
            invoices = invoices.Where(i => i.ProjectId == projectId);
        }

        var invoiceRows = await invoices
            .Select(i => new
            {
                i.Id,
                i.Number,
                i.ProjectId,
                ProjectName = i.Project.Name,
                Client = i.Project.Customer != null ? i.Project.Customer.Name : null,
                i.IssueDate,
                i.DueDate,
                i.Amount,
                Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
                BranchId = i.Project.BranchId,
            })
            .ToListAsync(cancellationToken);

        var rows = invoiceRows.Select(i =>
        {
            var (age, days) = DebtAge.Of(i.DueDate, today);

            return new DebtRowDto
            {
                Source = "Invoice",
                Id = i.Id,
                Title = i.Number,
                ProjectId = i.ProjectId,
                ProjectName = i.ProjectName,
                Counterparty = i.Client,
                IssuedOn = i.IssueDate,
                DueDate = i.DueDate,
                Amount = i.Amount,
                PaidAmount = i.Paid,
                OpenAmount = i.Amount - i.Paid,
                DaysOverdue = days,
                Age = age,
                BranchId = i.BranchId,
            };
        }).ToList();

        var incomes = _context.BookEntries.AsNoTracking()
            .InBranch(branchId)
            .Where(e => e.Kind == BookEntryKind.Income && e.PaidOn == null);

        if (request.ProjectId is { } projectFilter)
        {
            incomes = incomes.Where(e => e.ProjectId == projectFilter);
        }

        var entryRows = await incomes
            .Select(e => new
            {
                e.Id,
                e.Description,
                e.Counterparty,
                CustomerName = e.Customer != null ? e.Customer.Name : null,
                e.ProjectId,
                ProjectName = e.Project != null ? e.Project.Name : null,
                e.OccurredOn,
                e.DueDate,
                e.Amount,
                e.BranchId,
            })
            .ToListAsync(cancellationToken);

        rows.AddRange(entryRows.Select(e =>
        {
            var (age, days) = DebtAge.Of(e.DueDate, today);

            return new DebtRowDto
            {
                Source = "BookEntry",
                Id = e.Id,
                Title = e.Description ?? e.Counterparty ?? string.Empty,
                ProjectId = e.ProjectId,
                ProjectName = e.ProjectName,
                Counterparty = e.CustomerName ?? e.Counterparty,
                IssuedOn = e.OccurredOn,
                DueDate = e.DueDate,
                Amount = e.Amount,
                PaidAmount = 0m,
                OpenAmount = e.Amount,
                DaysOverdue = days,
                Age = age,
                BranchId = e.BranchId,
            };
        }));

        return DebtBuilder.Build(today, rows);
    }
}

/// <summary>What the firm still owes: costs entered in the book that are not marked paid, aged by due date.</summary>
public record GetPayablesQuery : IRequest<DebtsDto>
{
    public Guid? BranchId { get; init; }

    public Guid? ProjectId { get; init; }
}

public class GetPayablesQueryHandler : IRequestHandler<GetPayablesQuery, DebtsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISender _sender;

    public GetPayablesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _sender = sender;
    }

    public async Task<DebtsDto> Handle(GetPayablesQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var costs = _context.BookEntries.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.Kind == BookEntryKind.Cost && e.PaidOn == null);

        if (request.ProjectId is { } projectId)
        {
            costs = costs.Where(e => e.ProjectId == projectId);
        }

        var entries = await costs
            .Select(e => new
            {
                e.Id,
                e.Description,
                e.Counterparty,
                e.ProjectId,
                ProjectName = e.Project != null ? e.Project.Name : null,
                e.OccurredOn,
                e.DueDate,
                e.Amount,
                e.BranchId,
            })
            .ToListAsync(cancellationToken);

        var rows = entries.Select(e =>
        {
            var (age, days) = DebtAge.Of(e.DueDate, today);

            return new DebtRowDto
            {
                Source = "BookEntry",
                Id = e.Id,
                Title = e.Description ?? e.Counterparty ?? string.Empty,
                ProjectId = e.ProjectId,
                ProjectName = e.ProjectName,
                Counterparty = e.Counterparty,
                IssuedOn = e.OccurredOn,
                DueDate = e.DueDate,
                Amount = e.Amount,
                PaidAmount = 0m,
                OpenAmount = e.Amount,
                DaysOverdue = days,
                Age = age,
                BranchId = e.BranchId,
            };
        }).ToList();

        // What is owed to the workers: what each group earned in the last months' settlements, less what
        // was already paid out. Only for those who may work with payouts.
        // Pay is not tied to a site, so a filter by site leaves it out.
        if (request.ProjectId is null && BookRules.CanWrite(_currentUserService.Role))
        {
            for (var back = 0; back < 3; back++)
            {
                var month = new DateOnly(today.Year, today.Month, 1).AddMonths(-back);

                var payouts = await _sender.Send(
                    new Payouts.GetPayoutsQuery { Year = month.Year, Month = month.Month, BranchId = request.BranchId },
                    cancellationToken);

                foreach (var group in payouts.Groups.Where(g => g.Remaining > 0m))
                {
                    var planned = payouts.Items
                        .Where(i => i.Group == group.Group && i.Kind == "Final" && i.PaidOn is null)
                        .OrderBy(i => i.PayDate)
                        .Select(i => (DateOnly?)i.PayDate)
                        .FirstOrDefault();

                    var (age, days) = DebtAge.Of(planned, today);

                    rows.Add(new DebtRowDto
                    {
                        Source = "Payroll",
                        Id = Guid.Empty,
                        Title = $"{month.Year}-{month.Month:00}",
                        Counterparty = group.Group,
                        IssuedOn = month.AddMonths(1).AddDays(-1),
                        DueDate = planned,
                        Amount = group.Earned,
                        PaidAmount = group.Earned - group.Remaining,
                        OpenAmount = group.Remaining,
                        DaysOverdue = days,
                        Age = age,
                        BranchId = request.BranchId,
                    });
                }
            }
        }

        return DebtBuilder.Build(today, rows);
    }
}

/// <summary>Marks an income or a cost of the book as paid, as of a day.</summary>
public record MarkBookEntryPaidCommand : IRequest<BookEntryDto>
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? PaidOn { get; init; }

    public BookPaymentMethod? Method { get; init; }
}

public class MarkBookEntryPaidCommandValidator : AbstractValidator<MarkBookEntryPaidCommand>
{
    public MarkBookEntryPaidCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.PaidOn)
            .LessThanOrEqualTo(today).WithMessage("Something cannot have been paid in the future.")
            .When(x => x.PaidOn is not null);

        RuleFor(x => x.Method).IsInEnum().When(x => x.Method is not null);
    }
}

public class MarkBookEntryPaidCommandHandler : IRequestHandler<MarkBookEntryPaidCommand, BookEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkBookEntryPaidCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BookEntryDto> Handle(MarkBookEntryPaidCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var entry = await _context.BookEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BookEntry), request.Id);

        if (entry.Kind is not (BookEntryKind.Income or BookEntryKind.Cost))
        {
            throw new ConflictException("Only an income or a cost can be marked as paid.");
        }

        var paidOn = request.PaidOn ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        if (entry.Kind == BookEntryKind.Income && paidOn < entry.OccurredOn)
        {
            throw new ConflictException("An income cannot have been received before the day it is dated.");
        }

        entry.PaidOn = paidOn;
        entry.Method = request.Method ?? entry.Method;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.BookEntries
            .AsNoTracking()
            .Where(e => e.Id == entry.Id)
            .Select(BookEntryMapping.Projection)
            .SingleAsync(cancellationToken);
    }
}
