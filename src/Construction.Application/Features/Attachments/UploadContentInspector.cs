using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Construction.Application.Common.Exceptions;

namespace Construction.Application.Features.Attachments;

/// <summary>
/// Looks inside an uploaded file, beyond the first bytes that <see cref="AttachmentRules.MatchesSignature"/> reads.
/// </summary>
/// <remarks>
/// The first bytes only say what a file claims to be. This checks the few ways a file that claims to be a document can
/// still hurt the person who opens it: an archive that unpacks to gigabytes, a "Word file" that is not one, a PDF that
/// runs script, a picture that declares a canvas no browser can hold. It never runs or renders anything.
/// <para>
/// shortcut: the PDF test reads plain tokens, so a PDF that hides the name in an object stream or a hex escape gets
/// through, and an unknown macro inside a valid Office file does too. The antivirus is the second line for known
/// samples; upgrade to flattening (rasterising) PDFs and stripping macros if the office starts receiving files from
/// outside the company.
/// </para>
/// </remarks>
public static partial class UploadContentInspector
{
    /// <summary>Most entries an Office file may hold. A real document has a few dozen.</summary>
    public const int MaxZipEntries = 10_000;

    /// <summary>Most bytes an Office file may unpack to.</summary>
    public const long MaxUnpackedBytes = 200L * 1024 * 1024;

    /// <summary>Largest unpacked-to-packed ratio of one entry before it is treated as a bomb (small entries are exempt).</summary>
    public const int MaxCompressionRatio = 200;

    /// <summary>Most pixels a picture may declare (a 50-megapixel phone is about 50 million).</summary>
    public const long MaxPixels = 120_000_000;

    /// <summary>Longest side of a picture, in pixels.</summary>
    public const int MaxSide = 20_000;

    private static readonly string[] ForbiddenEntryExtensions =
        [".exe", ".dll", ".scr", ".bat", ".cmd", ".com", ".ps1", ".vbs", ".js", ".jar", ".msi", ".lnk"];

    [GeneratedRegex(@"/(JavaScript|JS|Launch|EmbeddedFile)(?![A-Za-z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex PdfActiveContent();

    /// <summary>
    /// Throws <see cref="ConflictException"/> when the file is unsafe to keep. The stream must be seekable; it is left
    /// at the position it started at.
    /// </summary>
    public static async Task InspectAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var start = content.Position;

        try
        {
            switch (extension)
            {
                case ".docx":
                    InspectOffice(content, "word/");
                    break;
                case ".xlsx":
                    InspectOffice(content, "xl/");
                    break;
                case ".pdf":
                    await InspectPdfAsync(content, cancellationToken);
                    break;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".webp":
                    await InspectImageAsync(content, extension, cancellationToken);
                    break;
            }
        }
        finally
        {
            content.Position = start;
        }
    }

    private static void InspectOffice(Stream content, string requiredFolder)
    {
        try
        {
            ReadOffice(content, requiredFolder);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or IOException or NotSupportedException)
        {
            // A file that only starts like an archive: the library reports it as whichever of these it tripped on (an
            // upload's own stream throws an ArgumentOutOfRangeException when the file is shorter than a zip's footer).
            throw new ConflictException("The content of the file does not match its type.");
        }
    }

    private static void ReadOffice(Stream content, string requiredFolder)
    {
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);

        if (archive.Entries.Count > MaxZipEntries)
        {
            throw new ConflictException("The file was refused: it holds too many parts.");
        }

        var hasContentTypes = false;
        var hasBody = false;
        long unpacked = 0;

        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName;

            hasContentTypes |= name.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase);
            hasBody |= name.StartsWith(requiredFolder, StringComparison.OrdinalIgnoreCase);

            unpacked += entry.Length;

            if (unpacked > MaxUnpackedBytes
                || (entry.Length > 1024 * 1024 && entry.Length / Math.Max(1, entry.CompressedLength) > MaxCompressionRatio))
            {
                throw new ConflictException("The file was refused: it unpacks to far more than it should.");
            }

            if (name.Contains("..", StringComparison.Ordinal)
                || name.Contains("vbaProject", StringComparison.OrdinalIgnoreCase)
                || name.Contains("activeX", StringComparison.OrdinalIgnoreCase)
                || ForbiddenEntryExtensions.Any(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("The file was refused: it contains macros or programs.");
            }
        }

        if (!hasContentTypes || !hasBody)
        {
            throw new ConflictException("The content of the file does not match its type.");
        }
    }

    private static async Task InspectPdfAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        // Latin-1 keeps every byte as one character, so the search cannot be thrown off by binary streams.
        var text = Encoding.Latin1.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);

        if (PdfActiveContent().IsMatch(text))
        {
            throw new ConflictException("The file was refused: the PDF contains script or an attached program.");
        }
    }

    private static async Task InspectImageAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        // A phone's EXIF, preview and colour profile can run past 64 KB before the frame header, so read generously.
        var header = new byte[1024 * 1024];
        var read = 0;

        while (read < header.Length)
        {
            var n = await content.ReadAsync(header.AsMemory(read), cancellationToken);

            if (n == 0)
            {
                break;
            }

            read += n;
        }

        var size = extension switch
        {
            ".png" => PngSize(header.AsSpan(0, read)),
            ".webp" => WebpSize(header.AsSpan(0, read)),
            _ => JpegSize(header.AsSpan(0, read))
        };

        // A picture whose size cannot be read is refused: the check is only worth anything if it cannot be skipped.
        if (size is not { } s || s.Width <= 0 || s.Height <= 0)
        {
            throw new ConflictException("The content of the file does not match its type.");
        }

        if (s.Width > MaxSide || s.Height > MaxSide || (long)s.Width * s.Height > MaxPixels)
        {
            throw new ConflictException("The picture is larger than the allowed size in pixels.");
        }
    }

    internal static (int Width, int Height)? PngSize(ReadOnlySpan<byte> h) =>
        h.Length >= 24 && h[12..16].SequenceEqual("IHDR"u8)
            ? (ReadInt32BigEndian(h[16..20]), ReadInt32BigEndian(h[20..24]))
            : null;

    internal static (int Width, int Height)? JpegSize(ReadOnlySpan<byte> h)
    {
        var i = 2;

        while (i + 9 < h.Length)
        {
            if (h[i] != 0xFF)
            {
                i++;
                continue;
            }

            var marker = h[i + 1];

            if (marker == 0xFF)
            {
                i++;
                continue;
            }

            // Start-of-frame markers carry the size; everything else is skipped by its length.
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return ((h[i + 7] << 8) | h[i + 8], (h[i + 5] << 8) | h[i + 6]);
            }

            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7))
            {
                i += 2;
                continue;
            }

            i += 2 + ((h[i + 2] << 8) | h[i + 3]);
        }

        return null;
    }

    internal static (int Width, int Height)? WebpSize(ReadOnlySpan<byte> h)
    {
        if (h.Length < 30)
        {
            return null;
        }

        var kind = h[12..16];

        if (kind.SequenceEqual("VP8X"u8))
        {
            return (1 + (h[24] | (h[25] << 8) | (h[26] << 16)), 1 + (h[27] | (h[28] << 8) | (h[29] << 16)));
        }

        if (kind.SequenceEqual("VP8L"u8) && h[20] == 0x2F)
        {
            var bits = h[21] | (h[22] << 8) | (h[23] << 16) | ((uint)h[24] << 24);
            return (1 + (int)(bits & 0x3FFF), 1 + (int)((bits >> 14) & 0x3FFF));
        }

        if (kind.SequenceEqual("VP8 "u8))
        {
            return ((h[26] | (h[27] << 8)) & 0x3FFF, (h[28] | (h[29] << 8)) & 0x3FFF);
        }

        return null;
    }

    private static int ReadInt32BigEndian(ReadOnlySpan<byte> b) =>
        (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
}
