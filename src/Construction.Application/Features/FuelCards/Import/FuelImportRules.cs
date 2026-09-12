namespace Construction.Application.Features.FuelCards.Import;

/// <summary>What may be uploaded as a fuel statement.</summary>
public static class FuelImportRules
{
    /// <summary>
    /// Largest file accepted. Mirrors <c>AttachmentRules.MaxSizeBytes</c>'s
    /// reasoning at a smaller ceiling: a monthly transaction statement is a
    /// spreadsheet of a few thousand rows at most, never tens of megabytes.
    /// </summary>
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    public static readonly string[] AllowedExtensions = [".xlsx", ".csv"];

    /// <summary>How many parsed rows the preview screen actually shows.</summary>
    public const int MaxPreviewRows = 50;

    public static bool HasAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
