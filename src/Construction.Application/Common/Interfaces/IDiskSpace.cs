namespace Construction.Application.Common.Interfaces;

/// <summary>How much of the disk the documents and the database share is left.</summary>
public interface IDiskSpace
{
    /// <summary>The disk the uploaded files live on, or null when it cannot be read (the check must never break a page).</summary>
    DiskSpaceReading? Read();
}

/// <summary>Total and free bytes of one disk.</summary>
public readonly record struct DiskSpaceReading(long TotalBytes, long FreeBytes)
{
    private const double Gigabyte = 1024d * 1024 * 1024;

    public double FreeFraction => TotalBytes <= 0 ? 1 : (double)FreeBytes / TotalBytes;

    public DiskSpaceLevel Level => DiskSpaceRules.LevelOf(this);

    /// <summary>Numbers only, so the same text reads in both languages: <c>7.6 GB free of 38.0 GB (20%)</c> without the words.</summary>
    public string Describe() =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{FreeBytes / Gigabyte:0.0} GB / {TotalBytes / Gigabyte:0.0} GB ({FreeFraction * 100:0}%)");
}

public enum DiskSpaceLevel
{
    Ok = 0,

    /// <summary>Time to act: clean up or grow the disk.</summary>
    Low = 1,

    /// <summary>Close to the point where uploads and the database start failing.</summary>
    Critical = 2
}

/// <summary>
/// When free space counts as low. Either a share of the disk or an absolute amount, whichever bites first: 20% of a
/// small disk is a few gigabytes, 20% of a big one is plenty.
/// </summary>
/// <remarks>
/// shortcut: fixed numbers. Move them to configuration when two installations need different ones.
/// </remarks>
public static class DiskSpaceRules
{
    public const double LowFraction = 0.20;
    public const long LowBytes = 8L * 1024 * 1024 * 1024;
    public const double CriticalFraction = 0.10;
    public const long CriticalBytes = 3L * 1024 * 1024 * 1024;

    public static DiskSpaceLevel LevelOf(DiskSpaceReading reading)
    {
        // A disk that reports no size is a disk that could not be read, not a full one.
        if (reading.TotalBytes <= 0)
        {
            return DiskSpaceLevel.Ok;
        }

        if (reading.FreeFraction < CriticalFraction || reading.FreeBytes < CriticalBytes)
        {
            return DiskSpaceLevel.Critical;
        }

        return reading.FreeFraction < LowFraction || reading.FreeBytes < LowBytes
            ? DiskSpaceLevel.Low
            : DiskSpaceLevel.Ok;
    }
}
