using Construction.Application.Common.Interfaces;

namespace Construction.IntegrationTests;

/// <summary>
/// A disk the tests can fill. Starts with plenty of room, so no test is affected by how full the machine it runs on is.
/// </summary>
public class FakeDiskSpace : IDiskSpace
{
    private const long Gigabyte = 1024L * 1024 * 1024;

    /// <summary>What <see cref="Read"/> returns; null stands for a disk that cannot be read.</summary>
    public DiskSpaceReading? Reading { get; set; } = new(100 * Gigabyte, 80 * Gigabyte);

    public DiskSpaceReading? Read() => Reading;

    public void SetFree(double freeGigabytes, double totalGigabytes = 40) =>
        Reading = new DiskSpaceReading((long)(totalGigabytes * Gigabyte), (long)(freeGigabytes * Gigabyte));

    public void Reset() => Reading = new DiskSpaceReading(100 * Gigabyte, 80 * Gigabyte);
}
