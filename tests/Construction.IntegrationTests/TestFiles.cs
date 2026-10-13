using System.Text;

namespace Construction.IntegrationTests;

/// <summary>
/// Bytes for an upload that pass the check that a file is what its name says. A test that only needs
/// "some file" gets the right first bytes for the extension followed by the text it wants to read back.
/// </summary>
public static class TestFiles
{
    public static byte[] WithSignature(string fileName, string text = "hello")
    {
        var body = Encoding.UTF8.GetBytes(text);

        byte[] head = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "%PDF-1.4\n"u8.ToArray(),
            ".jpg" or ".jpeg" => [0xFF, 0xD8, 0xFF, 0xE0],
            ".png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            ".docx" or ".xlsx" => [0x50, 0x4B, 0x03, 0x04],
            ".doc" or ".xls" => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1],
            _ => [],
        };

        return [.. head, .. body];
    }
}
