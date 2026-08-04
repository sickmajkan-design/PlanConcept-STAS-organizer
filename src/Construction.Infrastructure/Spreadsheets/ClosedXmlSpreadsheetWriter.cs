using ClosedXML.Excel;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;

namespace Construction.Infrastructure.Spreadsheets;

/// <summary>
/// Writes a real .xlsx rather than a CSV.
/// </summary>
/// <remarks>
/// CSV would need no dependency, and it was the obvious first choice. It does
/// not survive this audience: Excel in a Serbian locale expects a semicolon
/// delimiter and reads a comma-delimited file as one column per row, and it
/// opens a UTF-8 file as Windows-1250 unless it finds a byte-order mark, which
/// turns every š and ć into mojibake. A workbook has neither problem, and it
/// carries the number formats that let somebody sum a column of hours.
/// </remarks>
public sealed class ClosedXmlSpreadsheetWriter : ISpreadsheetWriter
{
    public string ContentType =>
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Write(Spreadsheet spreadsheet)
    {
        using var workbook = new XLWorkbook();

        foreach (var sheet in spreadsheet.Sheets)
        {
            AddSheet(workbook, sheet);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void AddSheet(XLWorkbook workbook, SpreadsheetSheet sheet)
    {
        var worksheet = workbook.Worksheets.Add(SafeSheetName(sheet.Name));

        for (var column = 0; column < sheet.Columns.Count; column++)
        {
            var cell = worksheet.Cell(1, column + 1);
            cell.Value = sheet.Columns[column].Header;
            cell.Style.Font.Bold = true;
        }

        for (var row = 0; row < sheet.Rows.Count; row++)
        {
            var values = sheet.Rows[row];

            for (var column = 0; column < sheet.Columns.Count; column++)
            {
                var value = column < values.Count ? values[column] : null;
                Fill(worksheet.Cell(row + 2, column + 1), value, sheet.Columns[column].Kind);
            }
        }

        if (sheet.Rows.Count > 0)
        {
            // The header stays put while the reader scrolls, and the filter
            // row is what makes an export usable rather than merely present.
            worksheet.SheetView.FreezeRows(1);
            worksheet.Range(1, 1, sheet.Rows.Count + 1, sheet.Columns.Count)
                .SetAutoFilter();
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void Fill(IXLCell cell, object? value, SpreadsheetValueKind kind)
    {
        if (value is null)
        {
            // Left genuinely empty rather than filled with a dash or a zero: a
            // reader summing the column must not be given a number nobody
            // recorded.
            return;
        }

        switch (kind)
        {
            case SpreadsheetValueKind.Money:
                cell.Value = Convert.ToDecimal(value);
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;

            case SpreadsheetValueKind.Quantity:
                cell.Value = Convert.ToDecimal(value);
                cell.Style.NumberFormat.Format = "#,##0.###";
                break;

            case SpreadsheetValueKind.Integer:
                cell.Value = Convert.ToInt64(value);
                cell.Style.NumberFormat.Format = "#,##0";
                break;

            case SpreadsheetValueKind.Date:
                cell.Value = value switch
                {
                    DateOnly date => date.ToDateTime(TimeOnly.MinValue),
                    DateTime instant => instant,
                    _ => cell.Value
                };
                cell.Style.DateFormat.Format = "dd.MM.yyyy.";
                break;

            case SpreadsheetValueKind.Duration:
                // Excel counts a day as 1.0, so minutes divide by 1440. The
                // square brackets stop a monthly total past 24 hours wrapping
                // back round to zero.
                cell.Value = Convert.ToDouble(value) / 1440d;
                cell.Style.NumberFormat.Format = "[h]:mm";
                break;

            default:
                cell.Value = value.ToString();
                break;
        }
    }

    /// <summary>
    /// Excel refuses a sheet name over 31 characters or containing any of
    /// <c>: \ / ? * [ ]</c>, and fails the whole save rather than the sheet.
    /// </summary>
    private static string SafeSheetName(string name)
    {
        var cleaned = new string(
            name.Where(c => !":\\/?*[]".Contains(c)).ToArray()).Trim();

        if (cleaned.Length == 0)
        {
            cleaned = "Sheet";
        }

        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
