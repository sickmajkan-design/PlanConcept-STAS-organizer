using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>One user-defined column of a <see cref="Ledger"/>, shared by every section in it.</summary>
public class LedgerColumn : BaseEntity, IAuditable
{
    public Guid LedgerId { get; set; }

    public Ledger Ledger { get; set; } = null!;

    public string Name { get; set; } = null!;

    public LedgerColumnDataType DataType { get; set; }

    /// <summary>
    /// Null means manual/free-typed (today's only behavior). When set, the
    /// column is read-only and its cell values are computed at read time
    /// from real platform data — never stored in <see cref="LedgerCell"/>.
    /// </summary>
    public LedgerColumnSourceMetric? SourceMetric { get; set; }

    /// <summary>
    /// How a computed column is worked out from the other columns of the same row,
    /// as JSON — see <c>LedgerFormula</c> in the Application layer. Null for an
    /// ordinary typed-in or sourced column.
    /// </summary>
    /// <remarks>
    /// Written only by a template, never by a user: a free-form formula editor
    /// would let a monthly payroll be broken by one wrong reference, which is
    /// exactly what the spreadsheet this replaces suffers from. A value typed
    /// into such a cell is a visible manual override.
    /// </remarks>
    public string? FormulaJson { get; set; }

    /// <summary>
    /// What a template column <em>is</em> (hours, client rate, …), independent of
    /// what it is called. Lets the checks find the hours column of a ledger
    /// whose owner has renamed it.
    /// </summary>
    public string? SystemKey { get; set; }

    public int SortOrder { get; set; }
}
