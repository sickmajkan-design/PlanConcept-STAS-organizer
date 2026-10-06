using Construction.Application.Common.Security;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelTransactions;

// ---- list ---------------------------------------------------------------

public record GetFuelTransactionsQuery : IPagedQuery, IRequest<PagedList<FuelTransactionDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    /// <summary>Any of these statuses; empty means all.</summary>
    public FuelTransactionStatus[]? Status { get; init; }

    public Guid? VehicleId { get; init; }

    public Guid? BatchId { get; init; }

    public string? CardNumber { get; init; }

    /// <summary>Matches the card number, the product, or the vehicle (name, plate or TD number).</summary>
    public string? Search { get; init; }
}

public class GetFuelTransactionsQueryValidator : PagedQueryValidator<GetFuelTransactionsQuery>
{
    public GetFuelTransactionsQueryValidator()
        : base(maxPageSize: 200)
    {
    }
}

public class GetFuelTransactionsQueryHandler
    : IRequestHandler<GetFuelTransactionsQuery, PagedList<FuelTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFuelTransactionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<FuelTransactionDto>> Handle(
        GetFuelTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see fuel statements.");
        }

        var query = _context.FuelTransactions.AsNoTracking();

        if (request.Status is { Length: > 0 } statuses)
        {
            query = query.Where(t => statuses.Contains(t.Status));
        }

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(t => t.VehicleId == vehicleId);
        }

        if (request.BatchId is { } batchId)
        {
            query = query.Where(t => t.ImportBatchId == batchId);
        }

        if (!string.IsNullOrWhiteSpace(request.CardNumber))
        {
            var card = request.CardNumber.Trim();
            query = query.Where(t => t.CardNumber.Contains(card));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(t =>
                EF.Functions.Like(t.CardNumber.ToLower(), pattern, SearchPattern.Escape) ||
                (t.ProductType != null && EF.Functions.Like(t.ProductType.ToLower(), pattern, SearchPattern.Escape)) ||
                (t.StatementVehicleLabel != null && EF.Functions.Like(t.StatementVehicleLabel.ToLower(), pattern, SearchPattern.Escape)) ||
                (t.Vehicle != null && (
                    EF.Functions.Like((t.Vehicle.Brand + " " + t.Vehicle.Model).ToLower(), pattern, SearchPattern.Escape) ||
                    EF.Functions.Like(t.Vehicle.RegistrationNumber.ToLower(), pattern, SearchPattern.Escape) ||
                    (t.Vehicle.TdNumber != null && EF.Functions.Like(t.Vehicle.TdNumber.ToLower(), pattern, SearchPattern.Escape)))));
        }

        return await PagedList<FuelTransactionDto>.CreateAsync(
            query
                .OrderByDescending(t => t.OccurredOn)
                .ThenByDescending(t => t.OccurredAtTime)
                .ThenBy(t => t.Id)
                .Select(FuelTransactionMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}

// ---- how many need attention -------------------------------------------

public record GetFuelTransactionCountsQuery : IRequest<Dictionary<string, int>>;

public class GetFuelTransactionCountsQueryHandler
    : IRequestHandler<GetFuelTransactionCountsQuery, Dictionary<string, int>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFuelTransactionCountsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Dictionary<string, int>> Handle(
        GetFuelTransactionCountsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see fuel statements.");
        }

        var counts = await _context.FuelTransactions
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Enum.GetValues<FuelTransactionStatus>()
            .ToDictionary(s => s.ToString(), s => counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0);
    }
}

// ---- batches ------------------------------------------------------------

public record GetFuelImportBatchesQuery : IPagedQuery, IRequest<PagedList<FuelImportBatchDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetFuelImportBatchesQueryValidator : PagedQueryValidator<GetFuelImportBatchesQuery>;

public class GetFuelImportBatchesQueryHandler
    : IRequestHandler<GetFuelImportBatchesQuery, PagedList<FuelImportBatchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFuelImportBatchesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<FuelImportBatchDto>> Handle(
        GetFuelImportBatchesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see fuel statements.");
        }

        return await PagedList<FuelImportBatchDto>.CreateAsync(
            _context.FuelImportBatches
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new FuelImportBatchDto
                {
                    Id = b.Id,
                    FileName = b.FileName,
                    ImportedAt = b.CreatedAt,
                    ImportedByEmail = _context.Users
                        .Where(u => u.Id == b.ImportedByUserId)
                        .Select(u => u.Email)
                        .FirstOrDefault(),
                    TotalRows = b.TotalRows,
                    NewCount = b.NewCount,
                    UpdatedCount = b.UpdatedCount,
                    DuplicateCount = b.DuplicateCount,
                }),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}

// ---- candidates ---------------------------------------------------------

/// <summary>The driver entries a statement row could be paired with by hand.</summary>
public record GetFuelExpenseCandidatesQuery(Guid TransactionId) : IRequest<List<FuelExpenseCandidateDto>>;

public class GetFuelExpenseCandidatesQueryHandler
    : IRequestHandler<GetFuelExpenseCandidatesQuery, List<FuelExpenseCandidateDto>>
{
    private const int WindowDays = 3;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFuelExpenseCandidatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<FuelExpenseCandidateDto>> Handle(
        GetFuelExpenseCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see fuel statements.");
        }

        var transaction = await _context.FuelTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken)
            ?? throw new NotFoundException(nameof(FuelTransaction), request.TransactionId);

        if (transaction.VehicleId is not { } vehicleId)
        {
            return [];
        }

        var from = transaction.OccurredOn.AddDays(-WindowDays);
        var to = transaction.OccurredOn.AddDays(WindowDays);

        var taken = _context.FuelTransactions
            .Where(t => t.VehicleExpenseId != null && t.Id != transaction.Id)
            .Select(t => t.VehicleExpenseId!.Value);

        return await _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.VehicleId == vehicleId
                && e.Kind == VehicleExpenseKind.Fuel
                && e.OccurredOn >= from && e.OccurredOn <= to
                && !taken.Contains(e.Id))
            .OrderBy(e => e.OccurredOn)
            .Select(e => new FuelExpenseCandidateDto
            {
                ExpenseId = e.Id,
                OccurredOn = e.OccurredOn,
                Amount = e.Amount,
                Litres = e.Litres,
                OdometerKm = e.OdometerKm,
                FuelProductType = e.FuelProductType,
            })
            .ToListAsync(cancellationToken);
    }
}

// ---- resolve ------------------------------------------------------------

public enum FuelTransactionResolution
{
    /// <summary>Pair the row with a driver entry chosen by hand.</summary>
    LinkExpense = 1,

    /// <summary>Record the fill-up from the statement, for a row no driver entered.</summary>
    CreateExpense = 2,

    /// <summary>Accept the row as it is despite the flagged difference.</summary>
    Confirm = 3,

    /// <summary>Set the row aside; it does not count against anyone.</summary>
    Ignore = 4
}

public record ResolveFuelTransactionCommand : IRequest<FuelTransactionDto>
{
    /// <summary>Set by the API layer from the route.</summary>
    public Guid Id { get; init; }

    public FuelTransactionResolution Resolution { get; init; }

    /// <summary>The driver entry to pair with, for <see cref="FuelTransactionResolution.LinkExpense"/>.</summary>
    public Guid? ExpenseId { get; init; }

    public string? Note { get; init; }

    /// <summary>
    /// Litres from the receipt, for <see cref="FuelTransactionResolution.CreateExpense"/>. The
    /// statement has none, and a fuel cost without litres is not allowed anywhere else either.
    /// </summary>
    public decimal? Litres { get; init; }

    public int? OdometerKm { get; init; }
}

public class ResolveFuelTransactionCommandValidator : AbstractValidator<ResolveFuelTransactionCommand>
{
    public ResolveFuelTransactionCommandValidator()
    {
        RuleFor(x => x.Resolution).IsInEnum();

        RuleFor(x => x.ExpenseId)
            .NotNull().WithMessage("Choose which driver entry to pair this row with.")
            .When(x => x.Resolution == FuelTransactionResolution.LinkExpense);

        // A confirmation or a dismissal is a judgement; the reason is the trail.
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason is required.")
            .When(x => x.Resolution is FuelTransactionResolution.Confirm or FuelTransactionResolution.Ignore);

        RuleFor(x => x.Litres)
            .NotNull().WithMessage("Litres are required to record a fill-up.")
            .GreaterThan(0).WithMessage("Litres must be greater than zero.")
            .When(x => x.Resolution == FuelTransactionResolution.CreateExpense);

        RuleFor(x => x.OdometerKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.OdometerKm is not null);

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class ResolveFuelTransactionCommandHandler
    : IRequestHandler<ResolveFuelTransactionCommand, FuelTransactionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _clock;

    public ResolveFuelTransactionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUserService = currentUserService;
        _clock = clock;
    }

    public async Task<FuelTransactionDto> Handle(
        ResolveFuelTransactionCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not settle fuel statements.");
        }

        var transaction = await _context.FuelTransactions
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FuelTransaction), request.Id);

        if (transaction.Status == FuelTransactionStatus.Matched)
        {
            throw new ConflictException("This row is already matched; there is nothing to settle.");
        }

        switch (request.Resolution)
        {
            case FuelTransactionResolution.LinkExpense:
                await LinkAsync(transaction, request.ExpenseId!.Value, cancellationToken);
                break;

            case FuelTransactionResolution.CreateExpense:
                CreateExpense(transaction, request.Litres!.Value, request.OdometerKm);
                break;

            case FuelTransactionResolution.Confirm:
                if (transaction.Status == FuelTransactionStatus.UnknownCard)
                {
                    throw new ConflictException("Assign the card to a vehicle first.");
                }

                break;

            case FuelTransactionResolution.Ignore:
                transaction.Status = FuelTransactionStatus.Ignored;
                transaction.VehicleExpenseId = null;
                break;
        }

        if (request.Resolution != FuelTransactionResolution.Ignore)
        {
            transaction.Status = FuelTransactionStatus.Resolved;
        }

        transaction.ResolutionNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        transaction.ResolvedByUserId = _currentUserService.UserId;
        transaction.ResolvedAt = _clock.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.FuelTransactions
            .AsNoTracking()
            .Where(t => t.Id == transaction.Id)
            .Select(FuelTransactionMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    private async Task LinkAsync(FuelTransaction transaction, Guid expenseId, CancellationToken cancellationToken)
    {
        var expense = await _context.VehicleExpenses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.Kind == VehicleExpenseKind.Fuel, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleExpense), expenseId);

        if (transaction.VehicleId != expense.VehicleId)
        {
            throw new ConflictException("That driver entry belongs to a different vehicle.");
        }

        var alreadyPaired = await _context.FuelTransactions.AnyAsync(
            t => t.VehicleExpenseId == expenseId && t.Id != transaction.Id, cancellationToken);

        if (alreadyPaired)
        {
            throw new ConflictException("That driver entry is already paired with another statement row.");
        }

        transaction.VehicleExpenseId = expenseId;
    }

    private void CreateExpense(FuelTransaction transaction, decimal litres, int? odometerKm)
    {
        if (transaction.VehicleId is not { } vehicleId)
        {
            throw new ConflictException("Assign the card to a vehicle first.");
        }

        var expense = new VehicleExpense
        {
            VehicleId = vehicleId,
            Kind = VehicleExpenseKind.Fuel,
            Amount = transaction.Amount,
            OccurredOn = transaction.OccurredOn,
            Litres = litres,
            OdometerKm = odometerKm,
            FuelProductType = transaction.ProductType,
            Supplier = "DKV",
            Note = $"Recorded by the office from the DKV statement (card {transaction.CardNumber}); no driver entry.",
            RecordedByUserId = _currentUserService.UserId,
        };

        _context.VehicleExpenses.Add(expense);
        transaction.VehicleExpense = expense;
    }
}

// ---- card assignment & re-check ----------------------------------------

/// <summary>
/// Puts a card on a vehicle (creating it, or moving it from the vehicle it was
/// wrongly on) and re-checks every open statement row.
/// </summary>
public record AssignDkvCardCommand : IRequest<int>
{
    public string CardNumber { get; init; } = null!;

    public Guid VehicleId { get; init; }
}

public class AssignDkvCardCommandValidator : AbstractValidator<AssignDkvCardCommand>
{
    public AssignDkvCardCommandValidator()
    {
        RuleFor(x => x.CardNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.VehicleId).NotEmpty();
    }
}

public class AssignDkvCardCommandHandler : IRequestHandler<AssignDkvCardCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AssignDkvCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <returns>How many open statement rows now sit in a different state than before.</returns>
    public async Task<int> Handle(AssignDkvCardCommand request, CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not manage fuel cards.");
        }

        if (!await _context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        var cardNumber = request.CardNumber.Trim();

        var card = await _context.FuelCards.FirstOrDefaultAsync(
            c => c.CardNumber.ToLower() == cardNumber.ToLower(), cancellationToken);

        if (card is null)
        {
            _context.FuelCards.Add(new FuelCard
            {
                VehicleId = request.VehicleId,
                Provider = "DKV",
                CardNumber = cardNumber,
            });
        }
        else
        {
            card.VehicleId = request.VehicleId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await DkvRecheck.RunAsync(_context, cancellationToken);
    }
}

/// <summary>Matches every open statement row again, e.g. after drivers have caught up.</summary>
public record RecheckDkvTransactionsCommand : IRequest<int>;

public class RecheckDkvTransactionsCommandHandler : IRequestHandler<RecheckDkvTransactionsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RecheckDkvTransactionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(RecheckDkvTransactionsCommand request, CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not settle fuel statements.");
        }

        return await DkvRecheck.RunAsync(_context, cancellationToken);
    }
}

internal static class DkvRecheck
{
    public static async Task<int> RunAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var plan = await DkvImportPlanner.PlanAsync(context, [], cancellationToken);
        var changed = 0;

        foreach (var item in plan.Items.Where(i => i is { RewritesMatch: true, Existing: not null }))
        {
            var existing = item.Existing!;

            if (existing.Status != item.Match.Status
                || existing.VehicleExpenseId != item.Match.ExpenseId
                || existing.Issue != item.Match.Issue)
            {
                changed++;
            }

            ImportDkvStatementCommandHandler.Apply(existing, item.Match);
        }

        await context.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
