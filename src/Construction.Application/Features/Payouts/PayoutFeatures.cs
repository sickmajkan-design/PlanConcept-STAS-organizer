using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Book;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Finance;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Payouts;

/// <summary>
/// Who may see and change what workers are paid out. Pay is the most sensitive figure in the system: it
/// needs an administrator and the full finance grant, and a person's name is shown only to those who may
/// already see pay.
/// </summary>
public static class PayoutRules
{
    public static async Task EnsureAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!BookRules.CanWrite(currentUserService.Role))
        {
            throw new ForbiddenAccessException("Only management may work with payouts.");
        }

        await BookRules.EnsureReadAsync(context, currentUserService, cancellationToken);
        await FinanceRules.EnsureFullAsync(context, currentUserService, cancellationToken);
    }

    public static readonly IReadOnlyList<PayoutGroup> Groups =
        [PayoutGroup.Workers, PayoutGroup.Subcontractors, PayoutGroup.Office];
}

public class WagePayoutDto
{
    public Guid Id { get; init; }

    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>"Workers", "Subcontractors" or "Office".</summary>
    public string Group { get; init; } = null!;

    public Guid? EmployeeId { get; init; }

    /// <summary>Null when the account may not see what individuals are paid.</summary>
    public string? EmployeeName { get; init; }

    /// <summary>"Advance" or "Final".</summary>
    public string Kind { get; init; } = null!;

    public DateOnly PayDate { get; init; }

    public decimal Amount { get; init; }

    public string? Method { get; init; }

    public DateOnly? PaidOn { get; init; }

    public string? Note { get; init; }

    public Guid? BranchId { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class PayoutGroupDto
{
    public string Group { get; init; } = null!;

    /// <summary>What the group earned in the month's settlement, after the advances typed in the sheet.</summary>
    public decimal Earned { get; init; }

    /// <summary>Advances recorded here, on top of any typed in the settlement.</summary>
    public decimal Advances { get; init; }

    /// <summary>Final payments already made.</summary>
    public decimal FinalPaid { get; init; }

    /// <summary>Final payments planned and not made yet.</summary>
    public decimal FinalPlanned { get; init; }

    /// <summary>What is still to be paid out: earned, less advances, less final payments made. Zero when overpaid.</summary>
    public decimal Remaining { get; init; }

    /// <summary>What was paid out beyond what was earned, carried into the next month.</summary>
    public decimal CarryOver { get; init; }

    /// <summary>The settlement rows of this group that make up <see cref="Earned"/>.</summary>
    public int RowCount { get; init; }
}

public class PayoutsDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>Whether there is a settlement for the month at all.</summary>
    public bool HasSettlement { get; init; }

    public IReadOnlyList<PayoutGroupDto> Groups { get; init; } = [];

    public decimal TotalEarned { get; init; }

    public decimal TotalRemaining { get; init; }

    public IReadOnlyList<WagePayoutDto> Items { get; init; } = [];
}

/// <summary>The month's payouts, per group, against what each group earned in the settlement.</summary>
public record GetPayoutsQuery : IRequest<PayoutsDto>
{
    public int Year { get; init; }

    public int Month { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetPayoutsQueryValidator : AbstractValidator<GetPayoutsQuery>
{
    public GetPayoutsQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetPayoutsQueryHandler : IRequestHandler<GetPayoutsQuery, PayoutsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPayoutsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PayoutsDto> Handle(GetPayoutsQuery request, CancellationToken cancellationToken)
    {
        await PayoutRules.EnsureAsync(_context, _currentUserService, cancellationToken);

        var canSeePay = await FinanceRules.CanSeePayAsync(_context, _currentUserService, cancellationToken);

        // What each group earned: the "pay" column of the month's settlement, row by row, grouped by the
        // row's own pay group.
        var ledgers = _context.Ledgers.AsNoTracking()
            .Where(l => l.Year == request.Year && l.Month == request.Month);

        if (request.BranchId is { } branchId)
        {
            var token = BranchTree.Token(branchId);
            ledgers = ledgers.Where(l => l.Branch!.Path.Contains(token));
        }

        var ledgerIds = await ledgers.Select(l => l.Id).ToListAsync(cancellationToken);

        var earned = PayoutRules.Groups.ToDictionary(g => g, _ => 0m);
        var rowCount = PayoutRules.Groups.ToDictionary(g => g, _ => 0);

        foreach (var ledgerId in ledgerIds)
        {
            var payColumn = await _context.LedgerColumns
                .AsNoTracking()
                .Where(c => c.LedgerId == ledgerId && c.SystemKey == LedgerTemplates.Keys.Pay)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (payColumn is not { } columnId)
            {
                continue;
            }

            var perRow = await LedgerColumnTotals.PerRowAsync(_context, ledgerId, columnId, cancellationToken);

            var groups = await _context.LedgerRows
                .AsNoTracking()
                .Where(r => r.Section.LedgerId == ledgerId)
                .Select(r => new { r.Id, r.PayGroup })
                .ToListAsync(cancellationToken);

            foreach (var row in groups)
            {
                earned[row.PayGroup] += perRow.TryGetValue(row.Id, out var value) ? value : 0m;
                rowCount[row.PayGroup]++;
            }
        }

        var payouts = _context.WagePayouts.AsNoTracking()
            .Where(p => p.Year == request.Year && p.Month == request.Month)
            .InBranch(request.BranchId);

        var items = await payouts
            .OrderBy(p => p.PayDate)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Year,
                p.Month,
                p.Group,
                p.EmployeeId,
                EmployeeName = p.Employee != null ? p.Employee.FirstName + " " + p.Employee.LastName : null,
                p.Kind,
                p.PayDate,
                p.Amount,
                p.Method,
                p.PaidOn,
                p.Note,
                p.BranchId,
                p.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        PayoutGroupDto Group(PayoutGroup group)
        {
            var advances = items.Where(i => i.Group == group && i.Kind == PayoutKind.Advance).Sum(i => i.Amount);
            var finalPaid = items
                .Where(i => i.Group == group && i.Kind == PayoutKind.Final && i.PaidOn != null)
                .Sum(i => i.Amount);
            var finalPlanned = items
                .Where(i => i.Group == group && i.Kind == PayoutKind.Final && i.PaidOn == null)
                .Sum(i => i.Amount);

            var balance = earned[group] - advances - finalPaid;

            return new PayoutGroupDto
            {
                Group = group.ToString(),
                Earned = earned[group],
                Advances = advances,
                FinalPaid = finalPaid,
                FinalPlanned = finalPlanned,
                Remaining = Math.Max(0m, balance),
                CarryOver = Math.Max(0m, -balance),
                RowCount = rowCount[group],
            };
        }

        var groupDtos = PayoutRules.Groups.Select(Group).ToList();

        return new PayoutsDto
        {
            Year = request.Year,
            Month = request.Month,
            HasSettlement = ledgerIds.Count > 0,
            Groups = groupDtos,
            TotalEarned = groupDtos.Sum(g => g.Earned),
            TotalRemaining = groupDtos.Sum(g => g.Remaining),
            Items = items.Select(i => new WagePayoutDto
            {
                Id = i.Id,
                Year = i.Year,
                Month = i.Month,
                Group = i.Group.ToString(),
                EmployeeId = canSeePay ? i.EmployeeId : null,
                EmployeeName = canSeePay ? i.EmployeeName : null,
                Kind = i.Kind.ToString(),
                PayDate = i.PayDate,
                Amount = i.Amount,
                Method = i.Method?.ToString(),
                PaidOn = i.PaidOn,
                Note = i.Note,
                BranchId = i.BranchId,
                CreatedAt = i.CreatedAt,
            }).ToList(),
        };
    }
}

public interface IWagePayoutInput
{
    int Year { get; }

    int Month { get; }

    PayoutGroup Group { get; }

    PayoutKind Kind { get; }

    DateOnly PayDate { get; }

    decimal Amount { get; }

    Guid? EmployeeId { get; }

    BookPaymentMethod? Method { get; }

    string? Note { get; }

    Guid? BranchId { get; }
}

internal sealed class WagePayoutInputValidator<T> : AbstractValidator<T>
    where T : IWagePayoutInput
{
    public WagePayoutInputValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = dateTimeProvider.Today;

        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Group).IsInEnum();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("An amount must be more than zero.")
            .LessThan(1_000_000_000m).WithMessage("That amount is too large.");

        // An advance is money already paid; only a final payment can be planned for a later day.
        RuleFor(x => x.PayDate)
            .LessThanOrEqualTo(today)
            .WithMessage("An advance is entered when it is paid, so it cannot be dated in the future.")
            .When(x => x.Kind == PayoutKind.Advance);

        RuleFor(x => x.Method).IsInEnum().When(x => x.Method is not null);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal static class WagePayoutRules
{
    public static async Task EnsureReferencesAsync(
        IApplicationDbContext context, IWagePayoutInput input, CancellationToken cancellationToken)
    {
        await BranchLookup.EnsureExistsAsync(context, input.BranchId, cancellationToken);

        if (input.EmployeeId is { } employeeId
            && !await context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }
    }

    public static Task<WagePayoutDto> ReadAsync(
        IApplicationDbContext context, Guid id, CancellationToken cancellationToken) =>
        context.WagePayouts
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new WagePayoutDto
            {
                Id = p.Id,
                Year = p.Year,
                Month = p.Month,
                Group = p.Group.ToString(),
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee != null ? p.Employee.FirstName + " " + p.Employee.LastName : null,
                Kind = p.Kind.ToString(),
                PayDate = p.PayDate,
                Amount = p.Amount,
                Method = p.Method == null ? null : p.Method.ToString(),
                PaidOn = p.PaidOn,
                Note = p.Note,
                BranchId = p.BranchId,
                CreatedAt = p.CreatedAt,
            })
            .FirstAsync(cancellationToken);

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Records an advance that was paid, or plans the final payment of a month.</summary>
public record CreateWagePayoutCommand : IRequest<WagePayoutDto>, IWagePayoutInput
{
    public int Year { get; init; }

    public int Month { get; init; }

    public PayoutGroup Group { get; init; }

    public PayoutKind Kind { get; init; }

    public DateOnly PayDate { get; init; }

    public decimal Amount { get; init; }

    public Guid? EmployeeId { get; init; }

    public BookPaymentMethod? Method { get; init; }

    public string? Note { get; init; }

    public Guid? BranchId { get; init; }
}

public class CreateWagePayoutCommandValidator : AbstractValidator<CreateWagePayoutCommand>
{
    public CreateWagePayoutCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        Include(new WagePayoutInputValidator<CreateWagePayoutCommand>(dateTimeProvider));
    }
}

public class CreateWagePayoutCommandHandler : IRequestHandler<CreateWagePayoutCommand, WagePayoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateWagePayoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<WagePayoutDto> Handle(CreateWagePayoutCommand request, CancellationToken cancellationToken)
    {
        await PayoutRules.EnsureAsync(_context, _currentUserService, cancellationToken);
        await WagePayoutRules.EnsureReferencesAsync(_context, request, cancellationToken);

        var payout = new WagePayout
        {
            Year = request.Year,
            Month = request.Month,
            Group = request.Group,
            Kind = request.Kind,
            PayDate = request.PayDate,
            Amount = request.Amount,
            EmployeeId = request.EmployeeId,
            Method = request.Method,
            // An advance is entered when it is paid; a final payment is paid when it is marked so.
            PaidOn = request.Kind == PayoutKind.Advance ? request.PayDate : null,
            Note = WagePayoutRules.Clean(request.Note),
            BranchId = request.BranchId,
            RecordedByUserId = _currentUserService.UserId,
        };

        _context.WagePayouts.Add(payout);
        await _context.SaveChangesAsync(cancellationToken);

        return await WagePayoutRules.ReadAsync(_context, payout.Id, cancellationToken);
    }
}

/// <summary>Corrects a payout. Its month, group and kind are fixed: remove it and enter another to move it.</summary>
public record UpdateWagePayoutCommand : IRequest<WagePayoutDto>
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    public DateOnly PayDate { get; init; }

    public decimal Amount { get; init; }

    public Guid? EmployeeId { get; init; }

    public BookPaymentMethod? Method { get; init; }

    public string? Note { get; init; }

    public Guid? BranchId { get; init; }
}

public class UpdateWagePayoutCommandValidator : AbstractValidator<UpdateWagePayoutCommand>
{
    public UpdateWagePayoutCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("An amount must be more than zero.")
            .LessThan(1_000_000_000m).WithMessage("That amount is too large.");
        RuleFor(x => x.Method).IsInEnum().When(x => x.Method is not null);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateWagePayoutCommandHandler : IRequestHandler<UpdateWagePayoutCommand, WagePayoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateWagePayoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WagePayoutDto> Handle(UpdateWagePayoutCommand request, CancellationToken cancellationToken)
    {
        await PayoutRules.EnsureAsync(_context, _currentUserService, cancellationToken);

        var payout = await _context.WagePayouts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WagePayout), request.Id);

        var today = _dateTimeProvider.Today;

        if ((payout.Kind == PayoutKind.Advance || payout.PaidOn is not null) && request.PayDate > today)
        {
            throw new ConflictException("Something already paid cannot be dated in the future.");
        }

        await Branches.BranchLookup.EnsureExistsAsync(_context, request.BranchId, cancellationToken);

        if (request.EmployeeId is { } employeeId
            && !await _context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        payout.PayDate = request.PayDate;
        payout.Amount = request.Amount;
        payout.EmployeeId = request.EmployeeId;
        payout.Method = request.Method;
        payout.Note = WagePayoutRules.Clean(request.Note);
        payout.BranchId = request.BranchId;

        if (payout.Kind == PayoutKind.Advance || payout.PaidOn is not null)
        {
            payout.PaidOn = request.PayDate;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await WagePayoutRules.ReadAsync(_context, payout.Id, cancellationToken);
    }
}

/// <summary>Removes a payout that was entered by mistake.</summary>
public record DeleteWagePayoutCommand(Guid Id) : IRequest;

public class DeleteWagePayoutCommandHandler : IRequestHandler<DeleteWagePayoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteWagePayoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteWagePayoutCommand request, CancellationToken cancellationToken)
    {
        await PayoutRules.EnsureAsync(_context, _currentUserService, cancellationToken);

        var payout = await _context.WagePayouts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WagePayout), request.Id);

        _context.WagePayouts.Remove(payout);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Marks a planned final payment as paid out.</summary>
public record MarkWagePayoutPaidCommand : IRequest<WagePayoutDto>
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? PaidOn { get; init; }

    public BookPaymentMethod? Method { get; init; }
}

public class MarkWagePayoutPaidCommandValidator : AbstractValidator<MarkWagePayoutPaidCommand>
{
    public MarkWagePayoutPaidCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = dateTimeProvider.Today;

        RuleFor(x => x.PaidOn)
            .LessThanOrEqualTo(today).WithMessage("Something cannot have been paid in the future.")
            .When(x => x.PaidOn is not null);

        RuleFor(x => x.Method).IsInEnum().When(x => x.Method is not null);
    }
}

public class MarkWagePayoutPaidCommandHandler : IRequestHandler<MarkWagePayoutPaidCommand, WagePayoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkWagePayoutPaidCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WagePayoutDto> Handle(MarkWagePayoutPaidCommand request, CancellationToken cancellationToken)
    {
        await PayoutRules.EnsureAsync(_context, _currentUserService, cancellationToken);

        var payout = await _context.WagePayouts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WagePayout), request.Id);

        if (payout.PaidOn is not null)
        {
            throw new ConflictException("This payout has already been paid.");
        }

        payout.PaidOn = request.PaidOn ?? _dateTimeProvider.Today;
        payout.Method = request.Method ?? payout.Method;

        await _context.SaveChangesAsync(cancellationToken);

        return await WagePayoutRules.ReadAsync(_context, payout.Id, cancellationToken);
    }
}
