using System.IO.Compression;
using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;

namespace Construction.UnitTests;

/// <summary>
/// What an upload is checked for beyond its first bytes: an archive that unpacks to far too much, a "Word file" that is
/// not one, script inside a PDF, a picture that declares a canvas no browser can hold.
/// </summary>
public class UploadContentInspectorTests
{
    private static Task Inspect(string fileName, byte[] bytes) =>
        UploadContentInspector.InspectAsync(new MemoryStream(bytes), fileName, CancellationToken.None);

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var entry = zip.CreateEntry(name, CompressionLevel.SmallestSize).Open();
                entry.Write(content);
            }
        }

        return stream.ToArray();
    }

    private static readonly byte[] ContentTypes = "<Types/>"u8.ToArray();

    // ---- Office files

    [Fact]
    public async Task A_real_word_file_passes()
    {
        await Inspect("a.docx", Zip(("[Content_Types].xml", ContentTypes), ("word/document.xml", "<w/>"u8.ToArray())));
    }

    [Fact]
    public async Task A_real_excel_file_passes()
    {
        await Inspect("a.xlsx", Zip(("[Content_Types].xml", ContentTypes), ("xl/workbook.xml", "<w/>"u8.ToArray())));
    }

    [Fact]
    public async Task A_file_that_only_starts_like_an_archive_is_refused()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            Inspect("fake.docx", [.. "PK\u0003\u0004"u8, .. "MZ this is a program"u8]));
    }

    [Fact]
    public async Task An_archive_without_the_parts_of_a_word_file_is_refused()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            Inspect("a.docx", Zip(("readme.txt", "hello"u8.ToArray()))));

        // An Excel file is not a Word file.
        await Assert.ThrowsAsync<ConflictException>(() =>
            Inspect("a.docx", Zip(("[Content_Types].xml", ContentTypes), ("xl/workbook.xml", "<w/>"u8.ToArray()))));
    }

    [Fact]
    public async Task A_zip_bomb_is_refused_by_what_it_unpacks_to()
    {
        // Two hundred and ten megabytes of zeros pack to about 200 KB.
        var bomb = Zip(
            ("[Content_Types].xml", ContentTypes),
            ("word/document.xml", new byte[210 * 1024 * 1024]));

        Assert.True(bomb.Length < 1024 * 1024, "the test file really is small");
        await Assert.ThrowsAsync<ConflictException>(() => Inspect("bomb.docx", bomb));
    }

    [Theory]
    [InlineData("word/vbaProject.bin")]
    [InlineData("word/activeX/activeX1.xml")]
    [InlineData("word/embeddings/payload.exe")]
    [InlineData("word/../../escape.xml")]
    public async Task Macros_programs_and_path_tricks_inside_an_office_file_are_refused(string entry)
    {
        var file = Zip(("[Content_Types].xml", ContentTypes), ("word/document.xml", "<w/>"u8.ToArray()), (entry, "x"u8.ToArray()));

        await Assert.ThrowsAsync<ConflictException>(() => Inspect("a.docx", file));
    }

    // ---- PDF

    [Fact]
    public async Task An_ordinary_pdf_passes_even_with_an_open_action_that_only_sets_the_view()
    {
        await Inspect("a.pdf", "%PDF-1.4\n1 0 obj<</Type/Catalog/OpenAction[3 0 R/Fit]>>endobj\n%%EOF"u8.ToArray());
    }

    [Theory]
    [InlineData("/S/JavaScript/JS(app.alert(1))")]
    [InlineData("/JS (x)")]
    [InlineData("/Launch /Win <<>>")]
    [InlineData("/EmbeddedFile")]
    public async Task A_pdf_with_script_or_an_attached_program_is_refused(string token)
    {
        var pdf = Encoding.Latin1.GetBytes($"%PDF-1.4\n1 0 obj<</Type/Catalog/OpenAction<<{token}>>>>endobj\n%%EOF");

        await Assert.ThrowsAsync<ConflictException>(() => Inspect("a.pdf", pdf));
    }

    [Fact]
    public async Task A_name_that_only_starts_like_a_script_token_is_not_script()
    {
        // /JSON-like and /JavaScripted are different names; only the exact names are active content.
        await Inspect("a.pdf", "%PDF-1.4\n<</JSONData 1 /JavaScriptFree 2>>\n%%EOF"u8.ToArray());
    }

    // ---- Pictures

    private static byte[] BigEndian(int value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] Png(int width, int height)
    {
        var b = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52 };
        b.AddRange(BigEndian(width));
        b.AddRange(BigEndian(height));
        b.AddRange(new byte[] { 8, 2, 0, 0, 0, 0, 0, 0, 0 });
        return [.. b];
    }

    private static byte[] Jpeg(int width, int height) =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
        0xFF, 0xC0, 0x00, 0x11, 0x08, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width,
        0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01
    ];

    [Fact]
    public async Task A_phone_sized_picture_passes()
    {
        await Inspect("a.png", Png(4000, 3000));
        await Inspect("a.jpg", Jpeg(8160, 6120));
    }

    [Fact]
    public async Task A_picture_that_declares_an_enormous_canvas_is_refused()
    {
        // 136 KB on disk, 144 million pixels in a browser.
        await Assert.ThrowsAsync<ConflictException>(() => Inspect("a.png", Png(12000, 12000)));
        await Assert.ThrowsAsync<ConflictException>(() => Inspect("a.jpg", Jpeg(15000, 12000)));
        await Assert.ThrowsAsync<ConflictException>(() => Inspect("a.png", Png(60000, 10)));
    }

    [Fact]
    public async Task A_picture_whose_size_cannot_be_read_is_refused()
    {
        // The JPEG signature followed by HTML, which is what a polyglot looks like.
        await Assert.ThrowsAsync<ConflictException>(() =>
            Inspect("poly.jpg", [0xFF, 0xD8, 0xFF, 0xE0, .. "<html><script>alert(1)</script></html>"u8]));
    }

    [Fact]
    public async Task A_jpeg_with_a_large_profile_ahead_of_the_frame_header_still_passes()
    {
        // A phone's EXIF and preview can run to a few hundred KB before the frame header.
        var header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0xFF, 0xFF };
        var padding = new byte[0xFFFF - 2];
        var second = new byte[] { 0xFF, 0xE2, 0xFF, 0xFF };
        var jpeg = new List<byte>(header);
        jpeg.AddRange(padding);
        jpeg.AddRange(second);
        jpeg.AddRange(new byte[0xFFFF - 2]);
        jpeg.AddRange(Jpeg(4000, 3000).Skip(2));

        await Inspect("a.jpg", [.. jpeg]);
    }

    [Fact]
    public async Task The_stream_is_left_where_it_started()
    {
        using var stream = new MemoryStream("%PDF-1.4\n%%EOF"u8.ToArray());

        await UploadContentInspector.InspectAsync(stream, "a.pdf", CancellationToken.None);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task Plain_text_and_old_office_formats_are_left_to_the_signature_check()
    {
        await Inspect("a.txt", "<script>alert(1)</script>"u8.ToArray());
        await Inspect("a.csv", "=cmd|' /C calc'!A0"u8.ToArray());
    }
}
