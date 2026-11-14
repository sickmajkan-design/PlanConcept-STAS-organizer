using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Construction.Application.Features.SystemStatus;

public class ContainerDto
{
    public string Name { get; init; } = string.Empty;

    public string Image { get; init; } = string.Empty;

    /// <summary>Docker's own word: running, exited, restarting, paused, created, dead.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>For example "Up 3 days (healthy)".</summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>The HTTPS certificate the proxy serves right now.</summary>
public class CertificateDto
{
    public DateTime NotAfterUtc { get; init; }

    public string Issuer { get; init; } = string.Empty;
}

/// <summary>A release that was running on the server: the commit it was built from and how far the database was.</summary>
public class ReleaseDto
{
    /// <summary>When the host script first saw it running (the most recent time, if it ran more than once).</summary>
    public DateTime SeenAtUtc { get; init; }

    /// <summary>The full commit hash; the images are tagged <c>sha-&lt;commit&gt;</c>.</summary>
    public string Commit { get; init; } = string.Empty;

    /// <summary>Database migrations applied when it started.</summary>
    public int Migrations { get; init; }
}

/// <summary>One basic hardening check of the host. The panel words it from <see cref="Key"/> and <see cref="Status"/>.</summary>
public class SecurityCheckDto
{
    /// <summary>sshPassword, sshRoot, fail2ban, firewall, updates, autoUpdates, reboot or ports.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary><c>ok</c>, <c>warn</c> (needs action) or <c>info</c> (worth knowing, no action implied).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>What was found: yes/no, active/missing, a count, or the ports.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>A second figure where one check has two (total pending updates, unexpected ports).</summary>
    public string Extra { get; init; } = string.Empty;
}

/// <summary>Docker's own summary of the images on the host, as text ("16.8GB (98%)").</summary>
public class DockerImagesDto
{
    public int Total { get; init; }

    public int Active { get; init; }

    public string Size { get; init; } = string.Empty;

    public string Reclaimable { get; init; } = string.Empty;
}

/// <summary>
/// What a script on the host wrote about Docker (<c>scripts/docker-status.sh</c>). The API never talks to Docker.
/// </summary>
public class DockerStatusDto
{
    /// <summary>False when the script has never written (not installed, or a development machine).</summary>
    public bool Known { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public List<ContainerDto> Containers { get; init; } = [];

    public DockerImagesDto? Images { get; init; }

    /// <summary>The last line of the nightly prune log, e.g. "2026-11-14T03:30Z freed 159 MB, 28057 MB free".</summary>
    public string? LastPrune { get; init; }

    /// <summary>Null until the script has read it (no domain configured, or no openssl on the host).</summary>
    public CertificateDto? Certificate { get; init; }

    /// <summary>Newest first, one row per commit, at most five. The first is the one running.</summary>
    public List<ReleaseDto> Releases { get; init; } = [];

    /// <summary>Empty until the host script has written <c>security.txt</c>.</summary>
    public List<SecurityCheckDto> Security { get; init; } = [];
}

/// <summary>Reads what <c>scripts/docker-status.sh</c> left behind.</summary>
public interface IDockerStatusReader
{
    DockerStatusDto Read();

    /// <summary>The raw lines of <c>metrics.log</c>, or null when the host script has not written it.</summary>
    string? ReadMetricsLog();
}

public static class DockerStatusParser
{
    /// <summary>Parses the JSON-lines files the script writes. A line that is not JSON is skipped, not fatal.</summary>
    public static DockerStatusDto Parse(string containersJsonl, string? dfJsonl, string? prune, DateTime updatedAtUtc, string? tls = null, string? releasesLog = null, string? security = null)
    {
        var containers = new List<ContainerDto>();

        foreach (var element in Lines(containersJsonl))
        {
            containers.Add(new ContainerDto
            {
                Name = Text(element, "Names"),
                Image = Text(element, "Image"),
                State = Text(element, "State"),
                Status = Text(element, "Status"),
            });
        }

        DockerImagesDto? images = null;

        foreach (var element in Lines(dfJsonl))
        {
            if (Text(element, "Type") == "Images")
            {
                images = new DockerImagesDto
                {
                    Total = int.TryParse(Text(element, "TotalCount"), out var total) ? total : 0,
                    Active = int.TryParse(Text(element, "Active"), out var active) ? active : 0,
                    Size = Text(element, "Size"),
                    Reclaimable = Text(element, "Reclaimable"),
                };
            }
        }

        return new DockerStatusDto
        {
            Known = true,
            UpdatedAtUtc = updatedAtUtc,
            Containers = containers.OrderBy(c => c.Name, StringComparer.Ordinal).ToList(),
            Images = images,
            LastPrune = string.IsNullOrWhiteSpace(prune) ? null : prune.Trim(),
            Certificate = ParseCertificate(tls),
            Releases = ParseReleases(releasesLog),
            Security = ParseSecurity(security),
        };
    }

    private static readonly string[] ExpectedPorts = ["22", "80", "443"];

    /// <summary>
    /// Turns the <c>key=value</c> lines into checks. A key the script could not read is simply not listed, so an unknown
    /// is never shown as fine.
    /// </summary>
    public static List<SecurityCheckDto> ParseSecurity(string? text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var at = line.IndexOf('=');

            if (at > 0)
            {
                values[line[..at]] = line[(at + 1)..].Trim();
            }
        }

        var checks = new List<SecurityCheckDto>();

        void Add(string key, string status, string value, string extra = "") =>
            checks.Add(new SecurityCheckDto { Key = key, Status = status, Value = value, Extra = extra });

        if (values.TryGetValue("ssh_password", out var password))
        {
            Add("sshPassword", password == "no" ? "ok" : "warn", password);
        }

        if (values.TryGetValue("ssh_root", out var root))
        {
            Add("sshRoot", root is "no" or "prohibit-password" or "without-password" or "forced-commands-only" ? "ok" : "warn", root);
        }

        if (values.TryGetValue("fail2ban", out var fail2ban))
        {
            Add("fail2ban", fail2ban == "active" ? "ok" : "warn", fail2ban);
        }

        if (values.TryGetValue("firewall", out var firewall))
        {
            // A cloud firewall in front of the host does the same job, so a host firewall that is off is not a verdict.
            Add("firewall", firewall == "active" ? "ok" : "info", firewall);
        }

        if (values.TryGetValue("updates_total", out var total) && int.TryParse(total, out var totalCount))
        {
            var security = values.TryGetValue("updates_security", out var s) && int.TryParse(s, out var securityCount) ? securityCount : 0;
            Add("updates", security > 0 ? "warn" : totalCount > 0 ? "info" : "ok", security.ToString(CultureInfo.InvariantCulture), totalCount.ToString(CultureInfo.InvariantCulture));
        }

        if (values.TryGetValue("auto_updates", out var auto))
        {
            Add("autoUpdates", auto == "enabled" ? "ok" : "warn", auto);
        }

        if (values.TryGetValue("reboot_required", out var reboot))
        {
            Add("reboot", reboot == "yes" ? "warn" : "ok", reboot);
        }

        if (values.TryGetValue("ports", out var ports) && ports.Length > 0)
        {
            var unexpected = ports.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(p => !ExpectedPorts.Contains(p)).ToList();
            Add("ports", unexpected.Count == 0 ? "ok" : "warn", ports, string.Join(",", unexpected));
        }

        return checks;
    }

    /// <summary>Reads <c>time|commit|migrations</c> lines (oldest first) into newest-first rows, one per commit.</summary>
    public static List<ReleaseDto> ParseReleases(string? log)
    {
        var rows = new List<ReleaseDto>();

        foreach (var line in (log ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|');

            if (parts.Length >= 2
                && DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var at)
                && Regex.IsMatch(parts[1], "^[0-9a-f]{40}$"))
            {
                rows.Add(new ReleaseDto
                {
                    SeenAtUtc = at,
                    Commit = parts[1],
                    Migrations = parts.Length > 2 && int.TryParse(parts[2], out var migrations) ? migrations : 0,
                });
            }
        }

        // A release that came back after a rollback is one row, at its latest time.
        return rows.AsEnumerable().Reverse().DistinctBy(r => r.Commit).Take(5).ToList();
    }

    /// <summary>Reads the two lines <c>openssl x509 -enddate -issuer</c> prints. Null when the end date is not there or not a date.</summary>
    public static CertificateDto? ParseCertificate(string? text)
    {
        DateTime? notAfter = null;
        var issuer = string.Empty;

        foreach (var line in (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("notAfter=", StringComparison.Ordinal))
            {
                // "Jan  5 12:00:00 2027 GMT": openssl pads a one-digit day with a second space.
                var value = Regex.Replace(line["notAfter=".Length..], @"\s+", " ");

                if (DateTime.TryParseExact(value, "MMM d HH:mm:ss yyyy 'GMT'", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    notAfter = parsed;
                }
            }
            else if (line.StartsWith("issuer=", StringComparison.Ordinal))
            {
                var raw = line["issuer=".Length..].Trim();
                var organisation = Regex.Match(raw, @"\bO\s*=\s*([^,]+)");
                issuer = organisation.Success ? organisation.Groups[1].Value.Trim() : raw;
            }
        }

        return notAfter is { } end ? new CertificateDto { NotAfterUtc = end, Issuer = issuer } : null;
    }

    private static IEnumerable<JsonElement> Lines(string? jsonl)
    {
        foreach (var line in (jsonl ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            JsonElement? parsed = null;

            try
            {
                parsed = JsonDocument.Parse(line).RootElement.Clone();
            }
            catch (JsonException)
            {
                // A half-written or unexpected line is not worth failing the whole page for.
            }

            if (parsed is { ValueKind: JsonValueKind.Object } element)
            {
                yield return element;
            }
        }
    }

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
}
