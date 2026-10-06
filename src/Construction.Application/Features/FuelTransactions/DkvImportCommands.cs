using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.FuelCards.Import;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.FuelTransactions;

public abstract record DkvImportCommandBase
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;
}

public abstract class DkvImportCommandValidator<T> : AbstractValidator<T>
    where T : DkvImportCommandBase
{
    protected DkvImportCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(FuelImportRules.HasAllowedExtension)
            .WithMessage("The statement must be an .xlsx or .csv file.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");
    }
}

/// <summary>Reads a DKV statement and reports what importing it would do. Writes nothing.</summary>
public record PreviewDkvImportCommand : DkvImportCommandBase, IRequest<DkvImportPreviewDto>;

public class PreviewDkvImportCommandValidator : DkvImportCommandValidator<PreviewDkvImportCommand>;

public class PreviewDkvImportCommandHandler : IRequestHandler<PreviewDkvImportCommand, DkvImportPreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuelStatementParser _xlsxParser;

    public PreviewDkvImportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFuelStatementParser xlsxParser)
    {
        _context = context;
        _currentUserService = currentUserService;
        _xlsxParser = xlsxParser;
    }

    public async Task<DkvImportPreviewDto> Handle(
        PreviewDkvImportCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not import fuel statements.");
        }

        var file = FuelStatementFileParser.Parse(request.FileName, request.Content, _xlsxParser);
        var parsed = DkvStatementParser.Parse(file.Rows);
        var plan = await DkvImportPlanner.PlanAsync(_context, parsed.Rows, cancellationToken);

        return DkvPreviewBuilder.Build(plan, parsed);
    }
}

internal static class DkvPreviewBuilder
{
    public static DkvImportPreviewDto Build(DkvPlan plan, DkvParseResult parsed)
    {
        var fileItems = plan.Items.Where(i => i.FromFile).ToList();
        var newItems = fileItems.Where(i => i.Outcome == DkvRowOutcome.New).ToList();

        string? NameOf(Guid? id) => id is { } v && plan.Vehicles.TryGetValue(v, out var info) ? info.Name : null;

        var needAttention = fileItems
            .Where(i => i.Match.Status != FuelTransactionStatus.Matched
                && (i.Outcome == DkvRowOutcome.New || i.Existing is { Status: not (FuelTransactionStatus.Resolved or FuelTransactionStatus.Ignored) }))
            .OrderBy(i => Severity(i.Match.Status))
            .ThenBy(i => i.Row.Date)
            .ToList();

        var unknownCards = fileItems
            .Where(i => i.Match.Status == FuelTransactionStatus.UnknownCard)
            .GroupBy(i => i.Row.CardNumber)
            .Select(g =>
            {
                var label = g.Select(i => i.Row.VehicleLabel).FirstOrDefault(l => !string.IsNullOrEmpty(l));
                DkvVehicleInfo? suggestion = null;

                if (label is not null)
                {
                    plan.VehiclesByTd.TryGetValue(label, out suggestion);
                }

                return new DkvUnknownCardDto
                {
                    CardNumber = g.Key,
                    StatementVehicleLabel = label,
                    RowCount = g.Count(),
                    TotalAmount = g.Sum(i => i.Row.Amount),
                    SuggestedVehicleId = suggestion?.VehicleId,
                    SuggestedVehicleName = suggestion?.Name,
                };
            })
            .ToList();

        return new DkvImportPreviewDto
        {
            TotalRows = parsed.Rows.Count + parsed.Errors.Count,
            NewCount = newItems.Count,
            DuplicateCount = fileItems.Count(i => i.Outcome == DkvRowOutcome.Duplicate),
            UpdatedCount = fileItems.Count(i => i.Outcome == DkvRowOutcome.Updated),
            MatchedCount = newItems.Count(i => i.Match.Status == FuelTransactionStatus.Matched),
            NeedsReviewCount = newItems.Count(i => i.Match.Status == FuelTransactionStatus.NeedsReview),
            NoDriverEntryCount = newItems.Count(i => i.Match.Status == FuelTransactionStatus.NoDriverEntry),
            UnknownCardCount = newItems.Count(i => i.Match.Status == FuelTransactionStatus.UnknownCard),
            NewAmount = newItems.Sum(i => i.Row.Amount),
            ParseErrors = parsed.Errors,
            UnknownCards = unknownCards,
            Rows = needAttention
                .Take(DkvImportLimits.MaxPreviewRows)
                .Select(i => new DkvPreviewRowDto
                {
                    RowNumber = i.Row.RowNumber,
                    CardNumber = i.Row.CardNumber,
                    VehicleId = i.Match.VehicleId,
                    VehicleName = NameOf(i.Match.VehicleId),
                    StatementVehicleLabel = i.Row.VehicleLabel,
                    OccurredOn = i.Row.Date,
                    OccurredAtTime = i.Row.Time,
                    ProductType = i.Row.ProductType,
                    Amount = i.Row.Amount,
                    Currency = i.Row.Currency,
                    Country = i.Row.Country,
                    IsInvoiced = i.Row.IsInvoiced,
                    Outcome = i.Outcome,
                    Status = i.Match.Status,
                    Issue = i.Match.Issue,
                    IssueDetail = i.Match.Detail,
                })
                .ToList(),
        };
    }

    private static int Severity(FuelTransactionStatus status) => status switch
    {
        FuelTransactionStatus.UnknownCard => 0,
        FuelTransactionStatus.NeedsReview => 1,
        FuelTransactionStatus.NoDriverEntry => 2,
        _ => 3
    };
}

/// <summary>
/// Stores a DKV statement and pairs each row with the driver's entry. Safe to
/// run twice on the same file, and on the next statement: a row already on file
/// is not added again, and one that has since been invoiced is updated.
/// </summary>
public record ImportDkvStatementCommand : DkvImportCommandBase, IRequest<DkvImportResultDto>;

public class ImportDkvStatementCommandValidator : DkvImportCommandValidator<ImportDkvStatementCommand>;

public class ImportDkvStatementCommandHandler : IRequestHandler<ImportDkvStatementCommand, DkvImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuelStatementParser _xlsxParser;
    private readonly INotificationService _notifications;

    public ImportDkvStatementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFuelStatementParser xlsxParser,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _xlsxParser = xlsxParser;
        _notifications = notifications;
    }

    public async Task<DkvImportResultDto> Handle(
        ImportDkvStatementCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanImportFuelStatements(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not import fuel statements.");
        }

        var file = FuelStatementFileParser.Parse(request.FileName, request.Content, _xlsxParser);
        var parsed = DkvStatementParser.Parse(file.Rows);

        var batch = new FuelImportBatch
        {
            FileName = Path.GetFileName(request.FileName),
            ImportedByUserId = _currentUserService.UserId,
            TotalRows = parsed.Rows.Count + parsed.Errors.Count,
        };

        var mismatches = new DkvMismatchCounts(0, 0, 0);

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                batch.NewCount = batch.UpdatedCount = batch.DuplicateCount = 0;

                var plan = await DkvImportPlanner.PlanAsync(_context, parsed.Rows, token);

                foreach (var item in plan.Items)
                {
                    if (item.Existing is null)
                    {
                        _context.FuelTransactions.Add(NewTransaction(batch, item));
                        batch.NewCount++;
                        continue;
                    }

                    var existing = item.Existing;

                    if (item.FromFile)
                    {
                        if (existing.IsInvoiced != item.Row.IsInvoiced)
                        {
                            existing.IsInvoiced = item.Row.IsInvoiced;
                            batch.UpdatedCount++;
                        }
                        else
                        {
                            batch.DuplicateCount++;
                        }
                    }

                    if (item.RewritesMatch)
                    {
                        Apply(existing, item.Match);
                    }
                }

                mismatches = DkvMismatchCounts.Of(plan.Items.Where(i => i.Existing is null));

                _context.FuelImportBatches.Add(batch);
                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        await DkvMismatchNotifier.NotifyAsync(
            _context, _notifications, _currentUserService.UserId, batch.Id, batch.FileName, mismatches, cancellationToken);

        return new DkvImportResultDto
        {
            BatchId = batch.Id,
            TotalRows = batch.TotalRows,
            NewCount = batch.NewCount,
            UpdatedCount = batch.UpdatedCount,
            DuplicateCount = batch.DuplicateCount,
            SkippedCount = parsed.Errors.Count,
        };
    }

    private static FuelTransaction NewTransaction(FuelImportBatch batch, DkvPlanItem item)
    {
        var row = item.Row;

        var transaction = new FuelTransaction
        {
            ImportBatch = batch,
            CardNumber = row.CardNumber,
            StatementVehicleLabel = row.VehicleLabel,
            OccurredOn = row.Date,
            OccurredAtTime = row.Time,
            ProductGroup = row.ProductGroup,
            ProductType = row.ProductType,
            ProductCode = row.ProductCode,
            Amount = row.Amount,
            Currency = row.Currency,
            Country = row.Country,
            IsInvoiced = row.IsInvoiced,
        };

        Apply(transaction, item.Match);

        return transaction;
    }

    /// <summary>Writes the matcher's verdict onto a row, leaving settled rows alone.</summary>
    internal static void Apply(FuelTransaction transaction, DkvMatch match)
    {
        transaction.VehicleId = match.VehicleId;
        transaction.VehicleExpenseId = match.ExpenseId;
        transaction.Status = match.Status;
        transaction.Issue = match.Issue;
        transaction.IssueDetail = match.Detail;
    }
}
