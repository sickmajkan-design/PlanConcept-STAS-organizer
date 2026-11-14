using Construction.Application.Features.SystemStatus;
using Microsoft.Extensions.Configuration;

namespace Construction.Infrastructure.Monitoring;

/// <summary>Reads <c>host/containers.jsonl</c>, <c>df.jsonl</c> and <c>prune.txt</c> from the folder the backups are in.</summary>
public sealed class DockerStatusReader : IDockerStatusReader
{
    private readonly string _directory;

    public DockerStatusReader(IConfiguration configuration)
    {
        _directory = Path.Combine(configuration["Backup:Directory"] ?? "/backups", "host");
    }

    public DockerStatusDto Read()
    {
        try
        {
            var containers = Path.Combine(_directory, "containers.jsonl");

            if (!File.Exists(containers))
            {
                return new DockerStatusDto();
            }

            return DockerStatusParser.Parse(
                File.ReadAllText(containers),
                ReadOrNull("df.jsonl"),
                ReadOrNull("prune.txt"),
                File.GetLastWriteTimeUtc(containers),
                ReadOrNull("tls.txt"),
                ReadOrNull("releases.log"),
                ReadOrNull("security.txt"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new DockerStatusDto();
        }
    }

    public string? ReadMetricsLog()
    {
        try
        {
            return ReadOrNull("metrics.log");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string? ReadOrNull(string name)
    {
        var path = Path.Combine(_directory, name);

        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
