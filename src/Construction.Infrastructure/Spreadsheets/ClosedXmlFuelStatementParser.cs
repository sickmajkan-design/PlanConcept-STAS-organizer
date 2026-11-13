using System.IO.Compression;
using ClosedXML.Excel;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.FuelCards.Import;

namespace Construction.Infrastructure.Spreadsheets;

/// <summary>Reads an uploaded fuel-statement workbook into plain cell strings.</summary>
public sealed class ClosedXmlFuelStatementParser : IFuelStatementParser
{
    /// <summary>
    /// Most bytes the workbook may unpack to. The upload limit is on the packed size, and a sheet of numbers packs to
    /// a tenth of that (300 000 rows are 3.6 MB packed and cost half a gigabyte to open), so the real limit is here.
    /// </summary>
    public const long MaxUnpackedBytes = 20L * 1024 * 1024;

    /// <summary>Most rows an import reads. A statement longer than this is split by month.</summary>
    public const int MaxRows = 50_000;

    public FuelStatementParseResult ParseWorkbook(Stream content)
    {
        try
        {
            return Read(content);
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A damaged, hostile or merely unusual workbook comes out of the library as whatever it tripped on
            // (a null reference, an XML error, a bad archive). All of those are "that file cannot be read", not a
            // fault of ours, and the person needs the sentence rather than a 500.
            throw new ConflictException("The spreadsheet could not be read. Save it again as .xlsx and retry.");
        }
    }

    private static FuelStatementParseResult Read(Stream content)
    {
        EnsureNotABomb(content);

        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<IReadOnlyList<string>>();

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return new FuelStatementParseResult(rows);
        }

        if (usedRange.RowCount() > MaxRows)
        {
            throw new ConflictException(
                $"The spreadsheet has more than {MaxRows:N0} rows. Split it (by month, for instance) and import the parts.");
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

    /// <summary>Adds up what the archive says it unpacks to, before anything is unpacked.</summary>
    private static void EnsureNotABomb(Stream content)
    {
        var start = content.CanSeek ? content.Position : 0;

        using (var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true))
        {
            long total = 0;

            foreach (var entry in archive.Entries)
            {
                total += entry.Length;

                if (total > MaxUnpackedBytes)
                {
                    throw new ConflictException(
                        "The spreadsheet is too large once opened. Split it (by month, for instance) and import the parts.");
                }
            }
        }

        if (content.CanSeek)
        {
            content.Position = start;
        }
    }
}
