using ClosedXML.Excel;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.Infrastructure.Spreadsheets;

/// <summary>
/// The house style every exported workbook shares — one palette, applied in
/// exactly one place, so "make the exports look professional" is a change
/// here rather than a change repeated across ten export handlers that know
/// nothing about fonts or fills.
/// </summary>
internal static class SpreadsheetTheme
{
    /// <summary>
    /// A dark neutral carries the header and totals bands — the accent color
    /// below is what actually reads as "the brand," used sparingly, the way a
    /// bank statement or a consulting deck uses its accent color for one rule
    /// or one row rather than painting whole panels with it. A solid orange
    /// header band read as a themed spreadsheet, not a financial document.
    /// </summary>
    public static readonly XLColor HeaderFill = XLColor.FromHtml("#1F2A37");
    public static readonly XLColor HeaderText = XLColor.White;

    /// <summary>The admin panel's own brand color (see construction_admin's theme.ts) — the same orange, not a coincidence. Used only as a thin accent rule.</summary>
    public static readonly XLColor AccentColor = XLColor.FromHtml("#E65100");

    public static readonly XLColor ZebraFill = XLColor.FromHtml("#F5F6F8");
    public static readonly XLColor BorderColor = XLColor.FromHtml("#D7DAE0");
    public static readonly XLColor FooterText = XLColor.FromHtml("#6B7280");
    public const string FontFamily = "Calibri";
}

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
            AddSheet(workbook, sheet, branding, spreadsheet.GeneratedAtLabel);
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

    /// <summary>
    /// The label a grand-total row's first cell carries, in both languages —
    /// how the writer recognizes one to give it its own styling without the
    /// ten export handlers that build totals rows needing to know that a
    /// "total row" is even a styling concept.
    /// </summary>
    private static readonly string[] TotalRowMarkers =
    [
        ExportLabels.Get("grandTotal", english: false),
        ExportLabels.Get("grandTotal", english: true),
    ];

    private static void AddSheet(
        XLWorkbook workbook, SpreadsheetSheet sheet, SpreadsheetBranding branding, string? generatedAtLabel)
    {
        var worksheet = workbook.Worksheets.Add(SafeSheetName(sheet.Name));
        worksheet.Style.Font.FontName = SpreadsheetTheme.FontFamily;
        worksheet.Style.Font.FontSize = 10.5;

        var headerRow = 1 + AddBrandingHeader(worksheet, sheet, branding);
        var columnCount = sheet.Columns.Count;

        for (var column = 0; column < columnCount; column++)
        {
            var cell = worksheet.Cell(headerRow, column + 1);
            cell.Value = sheet.Columns[column].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10.5;
            cell.Style.Font.FontColor = SpreadsheetTheme.HeaderText;
            cell.Style.Fill.BackgroundColor = SpreadsheetTheme.HeaderFill;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorderColor = SpreadsheetTheme.AccentColor;
        }

        worksheet.Row(headerRow).Height = 22;

        var lastDataRow = headerRow;

        for (var row = 0; row < sheet.Rows.Count; row++)
        {
            var values = sheet.Rows[row];
            var sheetRow = headerRow + row + 1;
            lastDataRow = sheetRow;
            var isTotalRow = values.Count > 0 && values[0] is string first && TotalRowMarkers.Contains(first);
            var isZebra = !isTotalRow && row % 2 == 1;

            for (var column = 0; column < columnCount; column++)
            {
                var value = column < values.Count ? values[column] : null;
                var cell = worksheet.Cell(sheetRow, column + 1);

                Fill(cell, value, sheet.Columns[column].Kind);

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = SpreadsheetTheme.BorderColor;
                cell.Style.Border.InsideBorderColor = SpreadsheetTheme.BorderColor;

                if (isTotalRow)
                {
                    // The same dark band the header uses, not a tint of the
                    // accent color — a total is the bottom bookend of the
                    // table, and pairing its styling with the header's is
                    // what makes it read that way rather than as one more
                    // (bolder) data row.
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = SpreadsheetTheme.HeaderText;
                    cell.Style.Fill.BackgroundColor = SpreadsheetTheme.HeaderFill;
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                    cell.Style.Border.TopBorderColor = SpreadsheetTheme.AccentColor;
                }
                else if (isZebra)
                {
                    cell.Style.Fill.BackgroundColor = SpreadsheetTheme.ZebraFill;
                }
            }
        }

        if (sheet.Rows.Count > 0)
        {
            // The header (and the identifying first column, for a sheet
            // wider than one screen) stay put while the reader scrolls, and
            // the filter row is what makes an export usable rather than
            // merely present.
            worksheet.SheetView.FreezeRows(headerRow);
            worksheet.SheetView.FreezeColumns(1);
            worksheet.Range(headerRow, 1, sheet.Rows.Count + headerRow, columnCount)
                .SetAutoFilter();
        }

        AddFooter(worksheet, generatedAtLabel, lastDataRow, columnCount);

        worksheet.Columns().AdjustToContents();

        for (var column = 1; column <= columnCount; column++)
        {
            // Breathing room past the tightest fit AdjustToContents gives.
            worksheet.Column(column).Width += 1.5;
        }

        // The drawn borders are the table now, not Excel's own faint default grid.
        worksheet.SetShowGridLines(false);

        // A document meant to be printed or turned into a PDF, not only
        // scrolled on screen: the header repeats on every page, and the
        // columns never spill onto a second sheet of paper.
        worksheet.PageSetup.PrintAreas.Clear();
        worksheet.PageSetup.PrintAreas.Add(1, 1, Math.Max(lastDataRow, headerRow), Math.Max(columnCount, 1));
        worksheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.PageSetup.Margins.SetLeft(0.4).SetRight(0.4).SetTop(0.6).SetBottom(0.6);
    }

    /// <summary>
    /// "Generated 12.09.2026. 21:15" beneath the table, small and muted — the
    /// one detail that turns a spreadsheet into a report: proof of exactly
    /// when it was taken, for whoever compares it against a later one.
    /// </summary>
    private static void AddFooter(
        IXLWorksheet worksheet, string? generatedAtLabel, int lastDataRow, int columnCount)
    {
        if (generatedAtLabel is null)
        {
            return;
        }

        var footerRow = lastDataRow + 2;
        var cell = worksheet.Cell(footerRow, 1);
        cell.Value = generatedAtLabel;
        cell.Style.Font.FontColor = SpreadsheetTheme.FooterText;
        cell.Style.Font.Italic = true;
        cell.Style.Font.FontSize = 9;

        if (columnCount > 1)
        {
            worksheet.Range(footerRow, 1, footerRow, columnCount).Merge();
        }
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

        // A letterhead band, not just bold text floating on the default
        // white: a filled, bordered row reads as the top of a document
        // rather than an accidental first line of data. Same dark band the
        // column headers use, with the same orange accent rule beneath it —
        // one visual language for the whole sheet, not the brand color
        // spent on two different things.
        var band = worksheet.Range(1, 1, 1, columnCount);
        band.Style.Fill.BackgroundColor = SpreadsheetTheme.HeaderFill;
        band.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
        band.Style.Border.BottomBorderColor = SpreadsheetTheme.AccentColor;
        worksheet.Row(1).Height = 28;

        if (branding.CompanyName is not null)
        {
            var nameCell = worksheet.Cell(1, 1);
            nameCell.Value = branding.CompanyName;
            nameCell.Style.Font.Bold = true;
            nameCell.Style.Font.FontSize = 14;
            nameCell.Style.Font.FontColor = SpreadsheetTheme.HeaderText;
            nameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            nameCell.Style.Alignment.Indent = 1;

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
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;

            case SpreadsheetValueKind.Quantity:
                cell.Value = Convert.ToDecimal(value);
                cell.Style.NumberFormat.Format = "#,##0.###";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;

            case SpreadsheetValueKind.Integer:
                cell.Value = Convert.ToInt64(value);
                cell.Style.NumberFormat.Format = "#,##0";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;

            case SpreadsheetValueKind.Date:
                cell.Value = value switch
                {
                    DateOnly date => date.ToDateTime(TimeOnly.MinValue),
                    DateTime instant => instant,
                    _ => cell.Value
                };
                cell.Style.DateFormat.Format = "dd.MM.yyyy.";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                break;

            case SpreadsheetValueKind.Duration:
                // Excel counts a day as 1.0, so minutes divide by 1440. The
                // square brackets stop a monthly total past 24 hours wrapping
                // back round to zero.
                cell.Value = Convert.ToDouble(value) / 1440d;
                cell.Style.NumberFormat.Format = "[h]:mm";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
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
