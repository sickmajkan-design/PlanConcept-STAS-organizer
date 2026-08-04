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
    IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>
/// A workbook, described without reference to any spreadsheet library.
/// </summary>
/// <remarks>
/// Built in the Application layer and rendered in Infrastructure, so the
/// feature handlers stay testable without a rendering dependency and the
/// choice of library stays replaceable.
/// </remarks>
public sealed record Spreadsheet(IReadOnlyList<SpreadsheetSheet> Sheets)
{
    public static Spreadsheet Of(SpreadsheetSheet sheet) => new([sheet]);
}
