using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelCards.Import;

/// <summary>
/// The write half of the statement import: creates a
/// <see cref="VehicleExpense"/> (<see cref="VehicleExpenseKind.Fuel"/>) for
/// every row that resolves cleanly. Safe to run twice on the same statement —
/// statements often overlap at month boundaries when re-imported — because a
/// row matching an existing expense on vehicle, date, amount and litres is
/// skipped as already imported rather than duplicated.
/// </summary>
public record ImportFuelTransactionsCommand : IRequest<FuelImportResultDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;

    public FuelImportColumnMapping Mapping { get; init; } = null!;

    public bool HasHeaderRow { get; init; } = true;
}

public class ImportFuelTransactionsCommandValidator : AbstractValidator<ImportFuelTransactionsCommand>
{
    public ImportFuelTransactionsCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(FuelImportRules.HasAllowedExtension)
            .WithMessage("The statement must be an .xlsx or .csv file.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");

        RuleFor(x => x.Mapping).NotNull();
    }
}

public class ImportFuelTransactionsCommandHandler
    : IRequestHandler<ImportFuelTransactionsCommand, FuelImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuelStatementParser _xlsxParser;

    public ImportFuelTransactionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFuelStatementParser xlsxParser)
    {
        _context = context;
        _currentUserService = currentUserService;
        _xlsxParser = xlsxParser;
    }

    public async Task<FuelImportResultDto> Handle(
        ImportFuelTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not import fuel statements.");
        }

        var parsed = FuelStatementFileParser.Parse(request.FileName, request.Content, _xlsxParser);
        var cardsByNumber = await FuelCardLookup.LoadByCardNumber(_context, cancellationToken);

        var results = FuelImportRowResolver.Resolve(
            parsed.Rows, request.Mapping, request.HasHeaderRow, cardsByNumber);

        var skipped = new List<FuelImportSkippedRowDto>();
        var toCreate = new List<VehicleExpense>();

        // Existing rows this vehicle already has, so a re-run of the same
        // statement recognises what it already imported.
        var vehicleIds = results
            .Where(r => r.Status == FuelImportRowStatus.Ready)
            .Select(r => r.VehicleId!.Value)
            .Distinct()
            .ToList();

        var existing = await _context.VehicleExpenses
            .Where(e => e.Kind == VehicleExpenseKind.Fuel && vehicleIds.Contains(e.VehicleId))
            .Select(e => new { e.VehicleId, e.OccurredOn, e.Amount, e.Litres })
            .ToListAsync(cancellationToken);

        var existingKeys = existing
            .Select(e => (e.VehicleId, e.OccurredOn, e.Amount, e.Litres))
            .ToHashSet();

        // Rows within the same statement that duplicate one another (a
        // re-uploaded file, or the source genuinely repeating a line) must
        // not both insert — the second one is exactly the case this dedup
        // exists for, just discovered before either has a database id.
        var stagedKeys = new HashSet<(Guid VehicleId, DateOnly OccurredOn, decimal Amount, decimal? Litres)>();

        foreach (var row in results)
        {
            if (row.Status != FuelImportRowStatus.Ready)
            {
                skipped.Add(new FuelImportSkippedRowDto
                {
                    RowNumber = row.RowNumber,
                    CardNumber = row.CardNumber,
                    Reason = row.Reason ?? row.Status.ToString(),
                });
                continue;
            }

            var key = (row.VehicleId!.Value, row.OccurredOn!.Value, row.Amount!.Value, row.Litres);

            if (existingKeys.Contains(key) || !stagedKeys.Add(key))
            {
                skipped.Add(new FuelImportSkippedRowDto
                {
                    RowNumber = row.RowNumber,
                    CardNumber = row.CardNumber,
                    Reason = "Already imported.",
                });
                continue;
            }

            var provider = cardsByNumber[row.CardNumber!.ToLowerInvariant()].Provider;

            toCreate.Add(new VehicleExpense
            {
                VehicleId = row.VehicleId.Value,
                Kind = VehicleExpenseKind.Fuel,
                Amount = row.Amount.Value,
                OccurredOn = row.OccurredOn.Value,
                Litres = row.Litres,
                OdometerKm = row.OdometerKm,
                FuelProductType = row.FuelProductType,
                Supplier = row.Supplier,
                Note = string.IsNullOrWhiteSpace(row.Note)
                    ? $"Imported from {provider} statement (card {row.CardNumber})"
                    : row.Note,
                RecordedByUserId = _currentUserService.UserId,
            });
        }

        if (toCreate.Count > 0)
        {
            await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    _context.VehicleExpenses.AddRange(toCreate);
                    await _context.SaveChangesAsync(token);
                },
                cancellationToken);
        }

        return new FuelImportResultDto
        {
            TotalRows = results.Count,
            CreatedCount = toCreate.Count,
            SkippedCount = skipped.Count,
            Skipped = skipped,
        };
    }
}
