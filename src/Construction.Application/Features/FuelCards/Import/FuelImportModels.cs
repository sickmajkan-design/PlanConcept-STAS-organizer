namespace Construction.Application.Features.FuelCards.Import;

/// <summary>
/// Which column of the uploaded statement holds which field, chosen by the
/// user on the mapping step since no two providers lay a statement out the
/// same way (and DKV's own layout is unknown until the company has API/portal
/// access to see a real sample).
/// </summary>
public record FuelImportColumnMapping
{
    public int CardNumberColumn { get; init; }

    public int OccurredOnColumn { get; init; }

    public int AmountColumn { get; init; }

    public int LitresColumn { get; init; }

    public int? SupplierColumn { get; init; }

    public int? NoteColumn { get; init; }

    public int? OdometerColumn { get; init; }

    public int? FuelProductTypeColumn { get; init; }
}

public enum FuelImportRowStatus
{
    Ready,
    MissingCardNumber,
    NoMatchingCard,
    InvalidDate,
    InvalidAmount,
    InvalidLitres,
    AlreadyImported
}

/// <summary>One parsed statement row, matched (or not) to a fuel card.</summary>
public class FuelImportRowResult
{
    /// <summary>1-based, counting the header row when there is one — what a user looking at the file in Excel would call it.</summary>
    public int RowNumber { get; init; }

    public string? CardNumber { get; init; }

    public Guid? VehicleId { get; init; }

    public string? VehicleName { get; init; }

    public DateOnly? OccurredOn { get; init; }

    public decimal? Amount { get; init; }

    public decimal? Litres { get; init; }

    public int? OdometerKm { get; init; }

    public string? FuelProductType { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }

    public FuelImportRowStatus Status { get; init; }

    /// <summary>Human-readable reason, set whenever <see cref="Status"/> is not <see cref="FuelImportRowStatus.Ready"/>.</summary>
    public string? Reason { get; init; }
}

public class FuelImportPreviewDto
{
    public int TotalRows { get; init; }

    public int ReadyCount { get; init; }

    public int ProblemCount { get; init; }

    /// <summary>The first <see cref="FuelImportRules.MaxPreviewRows"/> rows only — enough to sanity-check the mapping without rendering a whole statement.</summary>
    public IReadOnlyList<FuelImportRowResult> Rows { get; init; } = [];
}

public class FuelImportSkippedRowDto
{
    public int RowNumber { get; init; }

    public string? CardNumber { get; init; }

    public string Reason { get; init; } = null!;
}

public class FuelImportResultDto
{
    public int TotalRows { get; init; }

    public int CreatedCount { get; init; }

    public int SkippedCount { get; init; }

    public IReadOnlyList<FuelImportSkippedRowDto> Skipped { get; init; } = [];
}
