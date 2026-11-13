using Construction.Infrastructure.Monitoring;

namespace Construction.UnitTests;

/// <summary>The host's own figures, read from the three /proc files as text, so the parsing is tested without a Linux machine.</summary>
public class ProcFilesTests
{
    [Fact]
    public void Uptime_is_the_first_number_in_seconds()
    {
        Assert.Equal(1234567, ProcFiles.ParseUptime("1234567.89 987654.32\n"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a number")]
    public void Uptime_that_cannot_be_read_is_null(string? text)
    {
        Assert.Null(ProcFiles.ParseUptime(text));
    }

    [Fact]
    public void Load_is_the_first_three_numbers_whatever_follows()
    {
        var load = ProcFiles.ParseLoadAverage("0.15 0.10 0.05 1/234 5678\n");

        Assert.Equal(0.15, load!.Value.One);
        Assert.Equal(0.10, load.Value.Five);
        Assert.Equal(0.05, load.Value.Fifteen);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0.15 0.10")]
    [InlineData("a b c")]
    public void Load_that_cannot_be_read_is_null(string? text)
    {
        Assert.Null(ProcFiles.ParseLoadAverage(text));
    }

    [Fact]
    public void Memory_is_read_in_bytes_and_available_is_the_figure_to_use()
    {
        const string text = """
            MemTotal:        3865476 kB
            MemFree:          242000 kB
            MemAvailable:    1800000 kB
            Buffers:          100000 kB
            SwapTotal:       2097148 kB
            SwapFree:        2000000 kB
            """;

        var memory = ProcFiles.ParseMemInfo(text);

        Assert.Equal(3865476L * 1024, memory!.Value.TotalBytes);
        Assert.Equal(1800000L * 1024, memory.Value.AvailableBytes);
        Assert.Equal(2097148L * 1024, memory.Value.SwapTotalBytes);
        Assert.Equal(2000000L * 1024, memory.Value.SwapFreeBytes);
    }

    [Fact]
    public void A_machine_without_swap_reports_none_not_nothing()
    {
        var memory = ProcFiles.ParseMemInfo("MemTotal: 1000 kB\nMemAvailable: 500 kB\n");

        Assert.Equal(0, memory!.Value.SwapTotalBytes);
        Assert.Equal(0, memory.Value.SwapFreeBytes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("MemTotal: 1000 kB\n")]
    public void Memory_without_the_two_essential_lines_is_null(string? text)
    {
        Assert.Null(ProcFiles.ParseMemInfo(text));
    }
}
