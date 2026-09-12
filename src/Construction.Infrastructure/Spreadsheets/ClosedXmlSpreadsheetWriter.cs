using ClosedXML.Excel;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFileStorage _storage;

    /// <remarks>
    /// This writer is a singleton (see <c>DependencyInjection.AddServices</c>)
    /// and stateless, but stamping the company name/logo onto every export
    /// needs a database read, so it takes a scope factory rather than
    /// <see cref="IApplicationDbContext"/> directly — that would tie a
    /// singleton to the first request's scoped instance.
    /// </remarks>
    public ClosedXmlSpreadsheetWriter(IServiceScopeFactory scopeFactory, IFileStorage storage)
    {
        _scopeFactory = scopeFactory;
        _storage = storage;
    }

    public string ContentType =>
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Write(Spreadsheet spreadsheet)
    {
        var branding = LoadBranding();

        using var workbook = new XLWorkbook();

        foreach (var sheet in spreadsheet.Sheets)
        {
            AddSheet(workbook, sheet, branding);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    /// <summary>
    /// Reads the company name and logo bytes once per export.
    /// </summary>
    /// <remarks>
    /// Blocking on the async database/storage calls is deliberate here rather
    /// than making <see cref="ISpreadsheetWriter"/> async everywhere: Kestrel
    /// carries no synchronization context, so there is no deadlock risk, an
    /// export is a rare, explicit user action rather than a hot path, and it
    /// keeps every one of the export handlers untouched — this is a single
    /// shared entry point, and threading branding through fourteen handler
    /// constructors would be needless surface area for the same result.
    /// </remarks>
    private SpreadsheetBranding LoadBranding()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var settings = context.CompanySettings
            .AsNoTracking()
            .Select(c => new { c.Name, c.LogoStorageKey, c.LogoContentType })
            .FirstOrDefault();

        if (settings is null)
        {
            return new SpreadsheetBranding(null, null, null);
        }

        byte[]? logoBytes = null;

        if (settings.LogoStorageKey is not null)
        {
            using var logoStream = _storage.OpenReadAsync(settings.LogoStorageKey)
                .GetAwaiter().GetResult();

            if (logoStream is not null)
            {
                using var buffer = new MemoryStream();
                logoStream.CopyTo(buffer);
                logoBytes = buffer.ToArray();
            }
        }

        return new SpreadsheetBranding(
            string.IsNullOrWhiteSpace(settings.Name) ? null : settings.Name,
            logoBytes,
            settings.LogoContentType);
    }

    private static void AddSheet(XLWorkbook workbook, SpreadsheetSheet sheet, SpreadsheetBranding branding)
    {
        var worksheet = workbook.Worksheets.Add(SafeSheetName(sheet.Name));

        var headerRow = 1 + AddBrandingHeader(worksheet, sheet, branding);

        for (var column = 0; column < sheet.Columns.Count; column++)
        {
            var cell = worksheet.Cell(headerRow, column + 1);
            cell.Value = sheet.Columns[column].Header;
            cell.Style.Font.Bold = true;
        }

        for (var row = 0; row < sheet.Rows.Count; row++)
        {
            var values = sheet.Rows[row];

            for (var column = 0; column < sheet.Columns.Count; column++)
            {
                var value = column < values.Count ? values[column] : null;
                Fill(worksheet.Cell(headerRow + row + 1, column + 1), value, sheet.Columns[column].Kind);
            }
        }

        if (sheet.Rows.Count > 0)
        {
            // The header stays put while the reader scrolls, and the filter
            // row is what makes an export usable rather than merely present.
            worksheet.SheetView.FreezeRows(headerRow);
            worksheet.Range(headerRow, 1, sheet.Rows.Count + headerRow, sheet.Columns.Count)
                .SetAutoFilter();
        }

        worksheet.Columns().AdjustToContents();
    }

    /// <summary>
    /// Writes the company name, merged and bold, above the column headers,
    /// and embeds the logo beside it when one is set. Returns how many rows
    /// it used, so the caller can shift the rest of the sheet down.
    /// </summary>
    private static int AddBrandingHeader(
        IXLWorksheet worksheet, SpreadsheetSheet sheet, SpreadsheetBranding branding)
    {
        if (branding.CompanyName is null && branding.LogoBytes is null)
        {
            return 0;
        }

        var columnCount = Math.Max(sheet.Columns.Count, 1);

        if (branding.CompanyName is not null)
        {
            var nameCell = worksheet.Cell(1, 1);
            nameCell.Value = branding.CompanyName;
            nameCell.Style.Font.Bold = true;
            nameCell.Style.Font.FontSize = 14;

            if (columnCount > 1)
            {
                worksheet.Range(1, 1, 1, columnCount).Merge();
            }
        }

        if (branding.LogoBytes is { Length: > 0 })
        {
            using var logoStream = new MemoryStream(branding.LogoBytes);

            // Anchored on the header row rather than resized to it: a logo
            // squeezed into a 15-point row is unrecognisable, so it is left
            // at a sensible fixed height and simply overlaps the row below,
            // which is blank whitespace either way.
            worksheet.AddPicture(logoStream)
                .MoveTo(worksheet.Cell(1, columnCount + 1))
                .WithSize(120, 40);
        }

        return 1;
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
