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
    public void The_certificate_end_date_and_issuer_are_read_including_a_one_digit_day()
    {
        var certificate = DockerStatusParser.ParseCertificate(
            "notAfter=Jan  5 12:30:00 2027 GMT\nissuer=C = US, O = Let's Encrypt, CN = R11\n");

        Assert.NotNull(certificate);
        Assert.Equal(new DateTime(2027, 1, 5, 12, 30, 0, DateTimeKind.Utc), certificate.NotAfterUtc);
        Assert.Equal("Let's Encrypt", certificate.Issuer);
        Assert.Null(DockerStatusParser.ParseCertificate("notAfter=nonsense"));
        Assert.Null(DockerStatusParser.ParseCertificate(null));
    }

    [Fact]
    public void Releases_are_listed_newest_first_one_per_commit_and_a_bad_line_is_ignored()
    {
        var a = new string('a', 40);
        var b = new string('b', 40);
        var log =
            $"2026-11-10T10:00:00Z|{a}|118\n" +
            $"2026-11-12T10:00:00Z|{b}|119\n" +
            "garbage\n" +
            "2026-11-13T10:00:00Z|nothex|120\n" +
            $"2026-11-14T10:00:00Z|{a}|118\n";

        var releases = DockerStatusParser.ParseReleases(log);

        Assert.Equal([a, b], releases.Select(r => r.Commit));
        Assert.Equal(new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc), releases[0].SeenAtUtc);
        Assert.Equal(119, releases[1].Migrations);
        Assert.Empty(DockerStatusParser.ParseReleases(null));
    }

    [Fact]
    public void Only_the_five_newest_releases_are_kept()
    {
        var log = string.Join("\n", Enumerable.Range(1, 8).Select(i => $"2026-11-{i:00}T10:00:00Z|{new string((char)('a' + i), 40)}|1"));

        Assert.Equal(5, DockerStatusParser.ParseReleases(log).Count);
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
