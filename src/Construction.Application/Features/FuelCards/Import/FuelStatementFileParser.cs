using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using FluentValidation.Results;

namespace Construction.Application.Features.FuelCards.Import;

/// <summary>Every raw cell of an uploaded statement, row by row.</summary>
public record FuelStatementParseResult(IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// Turns an uploaded DKV-or-whoever-else statement into plain rows of cell
/// strings, so the mapping step and the importer never need to know whether
/// it came in as a workbook or a comma-separated file.
/// </summary>
/// <remarks>
/// There is no real DKV export to calibrate against yet (see the fuel-cards
/// feature notes) — the company has no API credentials from DKV's sales team,
/// so this reads whatever column layout a human tells it to via the mapping
/// step rather than assuming one. That is also why this accepts any
/// spreadsheet-shaped export, not just DKV's.
///
/// The .xlsx path is delegated to <see cref="IFuelStatementParser"/> since
/// ClosedXML lives in Infrastructure; the .csv path needs no such dependency
/// and is handled directly.
/// </remarks>
public static class FuelStatementFileParser
{
    public static FuelStatementParseResult Parse(
        string fileName, Stream content, IFuelStatementParser xlsxParser)
    {
        if (!FuelImportRules.HasAllowedExtension(fileName))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(fileName), "The statement must be an .xlsx or .csv file.")
            ]);
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension == ".csv"
            ? ParseCsv(content)
            : xlsxParser.ParseWorkbook(content);
    }

    /// <summary>
    /// A parser for straightforward exports, not a full RFC 4180
    /// implementation: it handles a quoted value containing a comma or an
    /// escaped <c>""</c>, which is what a spreadsheet's own CSV export
    /// produces, and nothing more exotic. Both <c>,</c> and <c>;</c> are
    /// accepted as the delimiter, since a European-locale export commonly
    /// uses the latter.
    /// </summary>
    private static FuelStatementParseResult ParseCsv(Stream content)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = reader.ReadToEnd();

        var rows = new List<IReadOnlyList<string>>();
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var cells = ParseCsvLine(line);

            if (cells.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                rows.Add(cells);
            }
        }

        return new FuelStatementParseResult(rows);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',' || c == ';')
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }
}
