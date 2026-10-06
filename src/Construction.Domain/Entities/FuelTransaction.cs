using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One line of a DKV statement, kept as the provider reported it.
/// </summary>
/// <remarks>
/// This is the provider's side of the books. What the driver recorded at the
/// pump (litres, odometer, the receipt photo) stays on the
/// <see cref="VehicleExpense"/>, and this row is checked against it rather than
/// replacing it. The statement carries no litres, so it cannot stand in for
/// that entry.
///
/// A row is identified by card, moment, product code and amount: DKV can bill
/// two products in the same minute (diesel and AdBlue), and the same row comes
/// back on a later statement once it goes from "not invoiced" to "invoiced".
/// </remarks>
public class FuelTransaction : BaseEntity, IAuditable
{
    public Guid ImportBatchId { get; set; }

    public FuelImportBatch ImportBatch { get; set; } = null!;

    public string CardNumber { get; set; } = null!;

    /// <summary>Null while the card is unknown.</summary>
    public Guid? VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    /// <summary>What the statement printed in its registration column, kept verbatim ("15", "SD SMART").</summary>
    public string? StatementVehicleLabel { get; set; }

    public DateOnly OccurredOn { get; set; }

    public TimeOnly OccurredAtTime { get; set; }

    public string? ProductGroup { get; set; }

    public string? ProductType { get; set; }

    public string ProductCode { get; set; } = "";

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    public string? Country { get; set; }

    /// <summary>The provider's own status: false while the line is still "not invoiced".</summary>
    public bool IsInvoiced { get; set; }

    public FuelTransactionStatus Status { get; set; }

    public FuelTransactionIssue Issue { get; set; }

    /// <summary>Human-readable detail of <see cref="Issue"/>, e.g. both amounts.</summary>
    public string? IssueDetail { get; set; }

    /// <summary>The driver entry this row was paired with.</summary>
    public Guid? VehicleExpenseId { get; set; }

    public VehicleExpense? VehicleExpense { get; set; }

    public string? ResolutionNote { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
