using Construction.Application.Common.Interfaces;
using Construction.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Construction.UnitTests;

/// <summary>When free space counts as low or close to out, how it reads, and that the real disk can be read.</summary>
public class DiskSpaceRulesTests
{
    private const long Gigabyte = 1024L * 1024 * 1024;

    private static DiskSpaceReading Disk(double freeGigabytes, double totalGigabytes) =>
        new((long)(totalGigabytes * Gigabyte), (long)(freeGigabytes * Gigabyte));

    [Theory]
    [InlineData(30, 40, DiskSpaceLevel.Ok)]        // 75% free
    [InlineData(9, 40, DiskSpaceLevel.Ok)]         // 22% and over 8 GB
    [InlineData(7.6, 38, DiskSpaceLevel.Low)]      // exactly the test server today: 20% and under 8 GB
    [InlineData(6, 40, DiskSpaceLevel.Low)]        // 15%
    [InlineData(100, 1000, DiskSpaceLevel.Low)]    // 10% of a big disk is 100 GB: still a share below the line
    [InlineData(3.9, 40, DiskSpaceLevel.Critical)] // just under 10%
    [InlineData(4, 40, DiskSpaceLevel.Low)]        // exactly 10%: low, not yet critical
    [InlineData(2.5, 100, DiskSpaceLevel.Critical)]
    [InlineData(0, 40, DiskSpaceLevel.Critical)]
    public void The_level_follows_the_share_and_the_amount_whichever_bites_first(
        double free, double total, DiskSpaceLevel expected)
    {
        Assert.Equal(expected, Disk(free, total).Level);
    }

    [Fact]
    public void A_big_disk_with_a_lot_left_in_absolute_terms_is_low_only_when_the_share_is()
    {
        // 15 GB free of 200 GB is 7.5%: critical by share though 15 GB is more than the absolute line.
        Assert.Equal(DiskSpaceLevel.Critical, Disk(15, 200).Level);

        // 50 GB free of 200 GB is 25%: fine.
        Assert.Equal(DiskSpaceLevel.Ok, Disk(50, 200).Level);
    }

    [Fact]
    public void It_reads_as_numbers_only_so_it_means_the_same_in_both_languages()
    {
        Assert.Equal("7.6 GB / 38.0 GB (20%)", Disk(7.6, 38).Describe());
    }

    [Fact]
    public void A_reading_with_no_size_is_not_alarming()
    {
        Assert.Equal(DiskSpaceLevel.Ok, new DiskSpaceReading(0, 0).Level);
    }

    [Fact]
    public void The_real_disk_under_a_folder_that_does_not_exist_yet_can_be_read()
    {
        var root = Path.Combine(Path.GetTempPath(), "construction-disk-test", Guid.NewGuid().ToString("N"), "storage");
        var reader = new DriveInfoDiskSpace(Options.Create(new FileStorageSettings { RootPath = root }));

        var reading = reader.Read();

        Assert.NotNull(reading);
        Assert.True(reading!.Value.TotalBytes > 0);
        Assert.InRange(reading.Value.FreeBytes, 0, reading.Value.TotalBytes);
    }
}
