using System.IO.Compression;
using ClosedXML.Excel;
using Construction.Application.Common.Exceptions;
using Construction.Infrastructure.Spreadsheets;

namespace Construction.UnitTests.Spreadsheets;

/// <summary>
/// The one reader every spreadsheet import goes through: an ordinary workbook is read, and a damaged, oversized or
/// hostile one is refused with a sentence instead of a crash or half a gigabyte of memory.
/// </summary>
public class ClosedXmlFuelStatementParserTests
{
    private static MemoryStream Workbook(int rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("S");

        for (var r = 1; r <= rows; r++)
        {
            sheet.Cell(r, 1).Value = $"item {r}";
            sheet.Cell(r, 2).Value = r;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream;
    }

    [Fact]
    public void An_ordinary_workbook_is_read_row_by_row()
    {
        var result = new ClosedXmlFuelStatementParser().ParseWorkbook(Workbook(3));

        Assert.Equal(3, result.Rows.Count);
        Assert.Equal(["item 1", "1"], result.Rows[0]);
    }

    [Fact]
    public void Bytes_that_are_not_a_workbook_are_refused_with_a_sentence()
    {
        var garbage = new MemoryStream("this is not a spreadsheet"u8.ToArray());

        Assert.Throws<ConflictException>(() => new ClosedXmlFuelStatementParser().ParseWorkbook(garbage));
    }

    [Fact]
    public void An_archive_that_is_not_a_workbook_is_refused_not_a_null_reference()
    {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = zip.CreateEntry("readme.txt").Open();
            entry.Write("hello"u8);
        }

        stream.Position = 0;

        Assert.Throws<ConflictException>(() => new ClosedXmlFuelStatementParser().ParseWorkbook(stream));
    }

    [Fact]
    public void A_workbook_that_unpacks_to_far_more_than_it_weighs_is_refused_before_it_is_opened()
    {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.SmallestSize).Open();
            entry.Write(new byte[(int)ClosedXmlFuelStatementParser.MaxUnpackedBytes + 1024 * 1024]);
        }

        Assert.True(stream.Length < 1024 * 1024, "the test file really is small");
        stream.Position = 0;

        var exception = Assert.Throws<ConflictException>(() => new ClosedXmlFuelStatementParser().ParseWorkbook(stream));
        Assert.Contains("too large", exception.Message);
    }

    [Fact]
    public void More_rows_than_the_limit_are_refused_with_advice_to_split()
    {
        var exception = Assert.Throws<ConflictException>(() =>
            new ClosedXmlFuelStatementParser().ParseWorkbook(Workbook(ClosedXmlFuelStatementParser.MaxRows + 1)));

        Assert.Contains("Split", exception.Message);
    }
}
