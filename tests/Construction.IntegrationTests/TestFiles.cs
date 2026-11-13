using System.IO.Compression;
using System.Text;

namespace Construction.IntegrationTests;

/// <summary>
/// Bytes for an upload that pass the check that a file is what its name says. A test that only needs
/// "some file" gets the right first bytes for the extension followed by the text it wants to read back.
/// Office files are a real (tiny) archive and pictures carry a real one-pixel header, because uploads are also
/// inspected inside, not only at the first bytes.
/// </summary>
public static class TestFiles
{
    public static byte[] WithSignature(string fileName, string text = "hello")
    {
        var body = Encoding.UTF8.GetBytes(text);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension is ".docx" or ".xlsx")
        {
            return Archive(extension == ".docx" ? "word/document.xml" : "xl/workbook.xml", body);
        }

        byte[] head = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "%PDF-1.4\n"u8.ToArray(),
            // SOI, a JFIF segment, then a start-of-frame that says 1 x 1.
            ".jpg" or ".jpeg" =>
            [
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
                0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x01, 0x00, 0x01, 0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01
            ],
            // Signature, then an IHDR chunk that says 1 x 1.
            ".png" =>
            [
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            ],
            ".doc" or ".xls" => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1],
            _ => [],
        };

        return [.. head, .. body];
    }

    /// <summary>A minimal valid Office archive holding <paramref name="body"/> in the named part.</summary>
    public static byte[] Archive(string bodyEntry, byte[] body)
    {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in new[]
                     {
                         ("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"/>"u8.ToArray()),
                         (bodyEntry, body)
                     })
            {
                using var entry = zip.CreateEntry(name).Open();
                entry.Write(content);
            }
        }

        return stream.ToArray();
    }
}
