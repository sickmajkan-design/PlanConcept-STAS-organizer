using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments;

namespace Construction.Application.Features.Branches;

/// <summary>What was stored for a logo: where, as what, and where its picture copy is (for an SVG).</summary>
public sealed record StoredLogo(string Key, string ContentType, string? RasterKey);

/// <summary>
/// How a logo, of a business unit or of the company, is checked and stored, so both follow the same
/// rules. A picture (png, jpg, webp) is kept as it is. An SVG is cleaned first, and the panel sends a
/// png copy of it along, because a spreadsheet and a phone draw pictures, not SVG.
/// </summary>
public static class LogoStorage
{
    public const string SvgContentType = "image/svg+xml";

    /// <summary>A logo is a small picture; this keeps the bar, the exports and the documents fast.</summary>
    public const long MaxSizeBytes = 2 * 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsSvg(string? contentType) => contentType == SvgContentType;

    /// <summary>The type a file name stands for, or null when it is not a logo type.</summary>
    public static string? ResolveContentType(string fileName)
    {
        if (string.Equals(Path.GetExtension(fileName), ".svg", StringComparison.OrdinalIgnoreCase))
        {
            return SvgContentType;
        }

        var contentType = AttachmentRules.ResolveContentType(fileName);

        return contentType is "image/png" or "image/jpeg" or "image/webp" ? contentType : null;
    }

    /// <summary>
    /// Stores the logo under <paramref name="key"/> (cleaning an SVG) and, for an SVG, its png copy under
    /// <paramref name="rasterKey"/>. A logo that is not an SVG removes any copy left from before.
    /// </summary>
    public static async Task<StoredLogo> SaveAsync(
        IFileStorage storage,
        string key,
        string rasterKey,
        string fileName,
        Stream content,
        Stream? raster,
        CancellationToken cancellationToken)
    {
        var contentType = ResolveContentType(fileName)
            ?? throw new ConflictException("The logo must be a png, jpg, webp or svg picture.");

        if (IsSvg(contentType))
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            if (!SvgLogoSanitizer.TrySanitize(buffer, out var clean))
            {
                throw new ConflictException("The SVG could not be read as a logo.");
            }

            await storage.SaveAsync(key, new MemoryStream(clean), contentType, cancellationToken);

            if (raster is not null)
            {
                await SaveRasterAsync(storage, rasterKey, raster, cancellationToken);

                return new StoredLogo(key, contentType, rasterKey);
            }

            // No copy came with it: the old one would show another drawing, so it goes.
            await storage.DeleteAsync(rasterKey, cancellationToken);

            return new StoredLogo(key, contentType, null);
        }

        await storage.SaveAsync(key, content, contentType, cancellationToken);
        await storage.DeleteAsync(rasterKey, cancellationToken);

        return new StoredLogo(key, contentType, null);
    }

    private static async Task SaveRasterAsync(
        IFileStorage storage,
        string rasterKey,
        Stream raster,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await raster.CopyToAsync(buffer, cancellationToken);

        var bytes = buffer.ToArray();

        if (bytes.Length == 0 || bytes.Length > MaxSizeBytes || !bytes.AsSpan().StartsWith(PngSignature))
        {
            throw new ConflictException("The picture copy of the SVG must be a png of at most 2 MB.");
        }

        await storage.SaveAsync(rasterKey, new MemoryStream(bytes), "image/png", cancellationToken);
    }

    /// <summary>
    /// A logo's bytes in a form a spreadsheet or a phone can draw: the picture itself, or the png copy of
    /// an SVG. Null when there is no such picture.
    /// </summary>
    public static async Task<(byte[] Bytes, string ContentType)?> ReadPictureAsync(
        IFileStorage storage,
        string? key,
        string? contentType,
        string? rasterKey,
        CancellationToken cancellationToken)
    {
        var (readKey, readType) = IsSvg(contentType) ? (rasterKey, "image/png") : (key, contentType);

        if (readKey is null)
        {
            return null;
        }

        await using var stream = await storage.OpenReadAsync(readKey, cancellationToken);

        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return (buffer.ToArray(), readType ?? "application/octet-stream");
    }
}
