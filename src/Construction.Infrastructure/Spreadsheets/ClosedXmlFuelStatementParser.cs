using ClosedXML.Excel;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.FuelCards.Import;

namespace Construction.Infrastructure.Spreadsheets;

/// <summary>Reads an uploaded fuel-statement workbook into plain cell strings.</summary>
public sealed class ClosedXmlFuelStatementParser : IFuelStatementParser
{
    public FuelStatementParseResult ParseWorkbook(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<IReadOnlyList<string>>();

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return new FuelStatementParseResult(rows);
        }

        var firstColumn = usedRange.FirstColumn().ColumnNumber();
        var lastColumn = usedRange.LastColumn().ColumnNumber();

        foreach (var row in usedRange.RowsUsed())
        {
            var cells = new List<string>();

            for (var column = firstColumn; column <= lastColumn; column++)
            {
                cells.Add(row.Cell(column).GetFormattedString().Trim());
            }

            // A blank trailing row (Excel often keeps one past the data)
            // carries nothing worth mapping and would otherwise show up in
            // the preview as an all-error row.
            if (cells.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                rows.Add(cells);
            }
        }

        return new FuelStatementParseResult(rows);
    }
}
