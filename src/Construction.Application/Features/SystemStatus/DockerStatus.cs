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
}

/// <summary>Reads what <c>scripts/docker-status.sh</c> left behind.</summary>
public interface IDockerStatusReader
{
    DockerStatusDto Read();
}

public static class DockerStatusParser
{
    /// <summary>Parses the JSON-lines files the script writes. A line that is not JSON is skipped, not fatal.</summary>
    public static DockerStatusDto Parse(string containersJsonl, string? dfJsonl, string? prune, DateTime updatedAtUtc, string? tls = null)
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
        };
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
