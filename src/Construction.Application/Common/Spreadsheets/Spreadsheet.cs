namespace Construction.Application.Common.Spreadsheets;

/// <summary>
/// How a cell should be written, and therefore what Excel will let the reader
/// do with it.
/// </summary>
/// <remarks>
/// This is the whole reason the export is a spreadsheet rather than a CSV. A
/// date written as text cannot be sorted chronologically, and a money column
/// written as text cannot be summed — which is the first thing anyone does
/// with an exported timesheet. The kind travels with the column so the writer
/// applies a real number format instead of guessing from the value.
/// </remarks>
public enum SpreadsheetValueKind
{
    Text,

    /// <summary>Two decimals, thousands grouped.</summary>
    Money,

    /// <summary>Up to three decimals, trailing zeroes suppressed.</summary>
    Quantity,

    Integer,

    /// <summary>Date only, in the reader's short date format.</summary>
    Date,

    /// <summary>
    /// Hours and minutes as a duration, not a time of day.
    /// </summary>
    /// <remarks>
    /// `[h]:mm` rather than `h:mm`: the square brackets stop Excel wrapping a
    /// total past 24 hours back round to zero, which is exactly what a monthly
    /// timesheet total does.
    /// </remarks>
    Duration
}

public sealed record SpreadsheetColumn(string Header, SpreadsheetValueKind Kind);

public sealed record SpreadsheetSheet(
    string Name,
    IReadOnlyList<SpreadsheetColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    IReadOnlySet<(int Row, int Column)>? ContractorCells = null);

/// <summary>The cells that name a subcontractor, which the writer shows in purple like the panel does.</summary>
public static class ContractorMarks
{
    /// <summary>One cell in <paramref name="column"/> for each row whose flag is true (rows counted from zero).</summary>
    public static IReadOnlySet<(int Row, int Column)> Of(IEnumerable<bool> flags, int column = 0) =>
        flags.Select((isContractor, row) => (isContractor, row))
            .Where(x => x.isContractor)
            .Select(x => (x.row, column))
            .ToHashSet();
}

/// <summary>
/// A workbook, described without reference to any spreadsheet library.
/// </summary>
/// <remarks>
/// Built in the Application layer and rendered in Infrastructure, so the
/// feature handlers stay testable without a rendering dependency and the
/// choice of library stays replaceable.
/// </remarks>
public sealed record Spreadsheet(
    IReadOnlyList<SpreadsheetSheet> Sheets,
    string? GeneratedAtLabel = null,
    Guid? BranchId = null)
{
    /// <summary>
    /// <paramref name="branchId"/> is the business unit the export is narrowed to: its logo and name head the
    /// sheet. Without one, the company's logo and name do.
    /// </summary>
    public static Spreadsheet Of(SpreadsheetSheet sheet, string? generatedAtLabel = null, Guid? branchId = null) =>
        new([sheet], generatedAtLabel, branchId);
}

/// <summary>
/// The platform owner's own name and logo, stamped onto every exported
/// workbook. Optional: an install that has never configured a company
/// profile still exports, just without a header.
/// </summary>
public sealed record SpreadsheetBranding(string? CompanyName, byte[]? LogoBytes, string? LogoContentType);
