using Construction.Application.Features.SystemStatus;

namespace Construction.UnitTests.SystemStatus;

public class DockerStatusParserTests
{
    private static readonly DateTime At = new(2026, 11, 14, 18, 0, 0, DateTimeKind.Utc);

    private const string Containers =
        "{\"Names\":\"construction-api-1\",\"Image\":\"ghcr.io/x/api:latest\",\"State\":\"running\",\"Status\":\"Up 3 minutes\"}\n" +
        "{\"Names\":\"construction-backup-1\",\"Image\":\"postgres:16-alpine\",\"State\":\"restarting\",\"Status\":\"Restarting (1) 5 seconds ago\"}\n";

    private const string Df =
        "{\"Type\":\"Images\",\"TotalCount\":\"15\",\"Active\":\"6\",\"Size\":\"2.1GB\",\"Reclaimable\":\"1.2GB (57%)\"}\n" +
        "{\"Type\":\"Containers\",\"TotalCount\":\"6\",\"Active\":\"6\",\"Size\":\"1MB\",\"Reclaimable\":\"0B (0%)\"}\n";

    [Fact]
    public void Containers_images_and_the_last_clean_up_are_read_and_sorted_by_name()
    {
        var status = DockerStatusParser.Parse(Containers, Df, "2026-11-14T03:30Z freed 159 MB, 28057 MB free\n", At);

        Assert.True(status.Known);
        Assert.Equal(At, status.UpdatedAtUtc);
        Assert.Equal(["construction-api-1", "construction-backup-1"], status.Containers.Select(c => c.Name));
        Assert.Equal("restarting", status.Containers[1].State);
        Assert.Equal(15, status.Images!.Total);
        Assert.Equal(6, status.Images.Active);
        Assert.Equal("1.2GB (57%)", status.Images.Reclaimable);
        Assert.Equal("2026-11-14T03:30Z freed 159 MB, 28057 MB free", status.LastPrune);
    }

    [Fact]
    public void A_broken_line_is_skipped_and_a_missing_summary_is_allowed()
    {
        var status = DockerStatusParser.Parse("{\"Names\":\"a\",\"State\":\"running\"}\n{ half a line", null, "  ", At);

        Assert.Single(status.Containers);
        Assert.Null(status.Images);
        Assert.Null(status.LastPrune);
    }
}
