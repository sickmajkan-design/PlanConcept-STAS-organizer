using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.FuelTransactions;

/// <summary>What would happen to a statement row on import.</summary>
public enum DkvRowOutcome
{
    New,

    /// <summary>Already on file, nothing changed.</summary>
    Duplicate,

    /// <summary>Already on file; the provider has since invoiced it.</summary>
    Updated
}

public class DkvPreviewRowDto
{
    public int RowNumber { get; init; }

    public string CardNumber { get; init; } = null!;

    public Guid? VehicleId { get; init; }

    public string? VehicleName { get; init; }

    public string? StatementVehicleLabel { get; init; }

    public DateOnly OccurredOn { get; init; }

    public TimeOnly OccurredAtTime { get; init; }

    public string? ProductType { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = null!;

    public string? Country { get; init; }

    public bool IsInvoiced { get; init; }

    public DkvRowOutcome Outcome { get; init; }

    public FuelTransactionStatus Status { get; init; }

    public FuelTransactionIssue Issue { get; init; }

    public string? IssueDetail { get; init; }
}

public class DkvUnknownCardDto
{
    public string CardNumber { get; init; } = null!;

    public string? StatementVehicleLabel { get; init; }

    public int RowCount { get; init; }

    public decimal TotalAmount { get; init; }

    /// <summary>A vehicle whose TD equals the label on the statement, offered as the likely owner.</summary>
    public Guid? SuggestedVehicleId { get; init; }

    public string? SuggestedVehicleName { get; init; }
}

public class DkvImportPreviewDto
{
    public int TotalRows { get; init; }

    public int NewCount { get; init; }

    public int DuplicateCount { get; init; }

    public int UpdatedCount { get; init; }

    public int MatchedCount { get; init; }

    public int NeedsReviewCount { get; init; }

    public int NoDriverEntryCount { get; init; }

    public int UnknownCardCount { get; init; }

    public decimal NewAmount { get; init; }

    public IReadOnlyList<DkvParseError> ParseErrors { get; init; } = [];

    public IReadOnlyList<DkvUnknownCardDto> UnknownCards { get; init; } = [];

    /// <summary>Rows a person has to look at, capped at <see cref="DkvImportLimits.MaxPreviewRows"/>.</summary>
    public IReadOnlyList<DkvPreviewRowDto> Rows { get; init; } = [];
}

public static class DkvImportLimits
{
    public const int MaxPreviewRows = 300;
}

public class DkvImportResultDto
{
    public Guid BatchId { get; init; }

    public int TotalRows { get; init; }

    public int NewCount { get; init; }

    public int UpdatedCount { get; init; }

    public int DuplicateCount { get; init; }

    public int SkippedCount { get; init; }
}

public class FuelTransactionDto
{
    public Guid Id { get; init; }

    public string CardNumber { get; init; } = null!;

    public Guid? VehicleId { get; init; }

    public string? VehicleName { get; init; }

    public string? VehicleTdNumber { get; init; }

    public string? StatementVehicleLabel { get; init; }

    public DateOnly OccurredOn { get; init; }

    public TimeOnly OccurredAtTime { get; init; }

    public string? ProductType { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = null!;

    public string? Country { get; init; }

    public bool IsInvoiced { get; init; }

    public string Status { get; init; } = null!;

    public string Issue { get; init; } = null!;

    public string? IssueDetail { get; init; }

    public Guid? VehicleExpenseId { get; init; }

    public decimal? ExpenseAmount { get; init; }

    public DateOnly? ExpenseOccurredOn { get; init; }

    public decimal? ExpenseLitres { get; init; }

    public string? ResolutionNote { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public Guid ImportBatchId { get; init; }
}

public class FuelImportBatchDto
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = null!;

    public DateTime ImportedAt { get; init; }

    public string? ImportedByEmail { get; init; }

    public int TotalRows { get; init; }

    public int NewCount { get; init; }

    public int UpdatedCount { get; init; }

    public int DuplicateCount { get; init; }
}

public class FuelExpenseCandidateDto
{
    public Guid ExpenseId { get; init; }

    public DateOnly OccurredOn { get; init; }

    public decimal Amount { get; init; }

    public decimal? Litres { get; init; }

    public int? OdometerKm { get; init; }

    public string? FuelProductType { get; init; }
}

public static class FuelTransactionMapping
{
    public static System.Linq.Expressions.Expression<Func<FuelTransaction, FuelTransactionDto>> Projection =>
        t => new FuelTransactionDto
        {
            Id = t.Id,
            CardNumber = t.CardNumber,
            VehicleId = t.VehicleId,
            VehicleName = t.Vehicle != null
                ? t.Vehicle.Brand + " " + t.Vehicle.Model + " (" + t.Vehicle.RegistrationNumber + ")"
                : null,
            VehicleTdNumber = t.Vehicle != null ? t.Vehicle.TdNumber : null,
            StatementVehicleLabel = t.StatementVehicleLabel,
            OccurredOn = t.OccurredOn,
            OccurredAtTime = t.OccurredAtTime,
            ProductType = t.ProductType,
            Amount = t.Amount,
            Currency = t.Currency,
            Country = t.Country,
            IsInvoiced = t.IsInvoiced,
            Status = t.Status.ToString(),
            Issue = t.Issue.ToString(),
            IssueDetail = t.IssueDetail,
            VehicleExpenseId = t.VehicleExpenseId,
            ExpenseAmount = t.VehicleExpense != null ? t.VehicleExpense.Amount : null,
            ExpenseOccurredOn = t.VehicleExpense != null ? t.VehicleExpense.OccurredOn : null,
            ExpenseLitres = t.VehicleExpense != null ? t.VehicleExpense.Litres : null,
            ResolutionNote = t.ResolutionNote,
            ResolvedAt = t.ResolvedAt,
            ImportBatchId = t.ImportBatchId,
        };
}
