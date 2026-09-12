using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One box in a <see cref="Ledger"/>'s month-summary panel — a small dashboard of
/// totals sitting above the section tables, mirroring the boxed KPI figures the
/// client keeps by hand in their own sheet (total income, fuel, regres,
/// contributions, fixed per-country overhead, and so on).
///
/// A box is either a live sum of one <see cref="LedgerColumn"/> across every row
/// in the ledger (<see cref="SourceColumnId"/> set) — always correct even as rows
/// change, unlike the client's own hand-copied cell references — or a manually
/// typed figure for a cost that never appears in the worker table at all, such as
/// a country's fixed monthly overhead (<see cref="SourceColumnId"/> null,
/// <see cref="ManualValue"/> used instead).
///
/// <see cref="Sign"/> says whether the box adds to or subtracts from the ledger's
/// automatically computed net total (income is +1, every cost is -1) — the
/// in-app equivalent of the client's own "ZARADA = UKUPAN PRIHOD minus every
/// other box" formula, computed instead of hand-maintained.
/// </summary>
public class LedgerSummaryBox : BaseEntity, IAuditable
{
    public Guid LedgerId { get; set; }

    public Ledger Ledger { get; set; } = null!;

    public string Label { get; set; } = null!;

    public Guid? SourceColumnId { get; set; }

    public LedgerColumn? SourceColumn { get; set; }

    public decimal? ManualValue { get; set; }

    /// <summary>+1 to add this box's value to the net total, -1 to subtract it.</summary>
    public int Sign { get; set; } = 1;

    /// <summary>Hex color for the box background (e.g. "#FBD98A"), or null for the default.</summary>
    public string? Color { get; set; }

    public int SortOrder { get; set; }
}
