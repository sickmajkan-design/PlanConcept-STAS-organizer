using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Construction.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure.Antivirus;

public class AntivirusSettings
{
    public const string SectionName = "Antivirus";

    /// <summary>Off by default: a fresh clone, CI and the tests need no scanner.</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "clamav";

    public int Port { get; set; } = 3310;

    /// <summary>How long one scan may take, connection included.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When the scanner cannot be reached: false refuses the upload (the safe default), true lets it
    /// through and logs. Only for a deployment that accepts the risk of a scanner outage.
    /// </summary>
    public bool FailOpen { get; set; }
}

/// <summary>
/// Scans with a ClamAV daemon over its <c>INSTREAM</c> protocol: the bytes are streamed to it in
/// length-prefixed chunks and it answers with one line.
/// </summary>
/// <remarks>
/// The protocol is small enough to speak directly, which avoids a dependency for a few dozen lines.
/// The file is read from where the stream stands and the position is restored afterwards.
/// </remarks>
public class ClamAvScanner : IVirusScanner
{
    private const int ChunkSize = 64 * 1024;

    /// <summary>How long a "yes" or "no" about reachability is reused, so a screen that asks every minute costs one ping.</summary>
    private static readonly TimeSpan ReachabilityAge = TimeSpan.FromSeconds(15);

    private readonly AntivirusSettings _settings;
    private readonly ILogger<ClamAvScanner> _logger;
    private readonly object _reachability = new();
    private bool _reachable = true;
    private DateTime _checkedAt = DateTime.MinValue;

    public ClamAvScanner(IOptions<AntivirusSettings> settings, ILogger<ClamAvScanner> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    /// <summary>
    /// <c>PING</c> and <c>PONG</c>. A scanner that was killed leaves its container up, so "the container runs"
    /// says nothing; only an answer does.
    /// </summary>
    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return true;
        }

        lock (_reachability)
        {
            if (DateTime.UtcNow - _checkedAt < ReachabilityAge)
            {
                return _reachable;
            }
        }

        var reachable = false;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));

            using var client = new TcpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, timeout.Token);

            await using var network = client.GetStream();
            await network.WriteAsync(Encoding.ASCII.GetBytes("zPING\0"), timeout.Token);

            reachable = string.Equals(await ReadReplyAsync(network, timeout.Token), "PONG", StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The virus scanner at {Host}:{Port} does not answer.", _settings.Host, _settings.Port);
        }

        lock (_reachability)
        {
            _reachable = reachable;
            _checkedAt = DateTime.UtcNow;
        }

        return reachable;
    }

    public async Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return new VirusScanResult(VirusScanOutcome.Clean);
        }

        var start = content.CanSeek ? content.Position : 0;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            return await ScanCoreAsync(content, timeout.Token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "The virus scanner at {Host}:{Port} could not scan a file.",
                _settings.Host, _settings.Port);

            if (_settings.FailOpen)
            {
                _logger.LogWarning("Antivirus FailOpen is on: the file is let through unscanned.");
                return new VirusScanResult(VirusScanOutcome.Clean);
            }

            return new VirusScanResult(VirusScanOutcome.Unavailable);
        }
        finally
        {
            if (content.CanSeek)
            {
                content.Position = start;
            }
        }
    }

    private async Task<VirusScanResult> ScanCoreAsync(Stream content, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, cancellationToken);

        await using var network = client.GetStream();

        // "z" asks for NUL-terminated commands and replies.
        await network.WriteAsync(Encoding.ASCII.GetBytes("zINSTREAM\0"), cancellationToken);

        var buffer = new byte[ChunkSize];
        var length = new byte[4];

        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            BinaryPrimitives.WriteInt32BigEndian(length, read);
            await network.WriteAsync(length, cancellationToken);
            await network.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        // A zero-length chunk ends the stream.
        BinaryPrimitives.WriteInt32BigEndian(length, 0);
        await network.WriteAsync(length, cancellationToken);
        await network.FlushAsync(cancellationToken);

        var reply = await ReadReplyAsync(network, cancellationToken);

        return Interpret(reply);
    }

    private static async Task<string> ReadReplyAsync(NetworkStream network, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        var one = new byte[1];

        while (bytes.Count < 4096)
        {
            var read = await network.ReadAsync(one, cancellationToken);

            if (read == 0 || one[0] == 0)
            {
                break;
            }

            bytes.Add(one[0]);
        }

        return Encoding.UTF8.GetString(bytes.ToArray()).Trim();
    }

    /// <summary>
    /// <c>stream: OK</c>, <c>stream: Eicar-Test-Signature FOUND</c> or <c>... ERROR</c>. Anything that is
    /// not one of the first two is a failure to scan, never a pass.
    /// </summary>
    internal static VirusScanResult Interpret(string reply)
    {
        if (reply.EndsWith("OK", StringComparison.Ordinal))
        {
            return new VirusScanResult(VirusScanOutcome.Clean);
        }

        if (reply.EndsWith("FOUND", StringComparison.Ordinal))
        {
            var name = reply.Replace("stream:", string.Empty, StringComparison.Ordinal)
                .Replace("FOUND", string.Empty, StringComparison.Ordinal)
                .Trim();

            return new VirusScanResult(VirusScanOutcome.Infected, name);
        }

        throw new InvalidOperationException($"Unexpected reply from the virus scanner: '{reply}'.");
    }
}

/// <summary>The scanner when scanning is not configured.</summary>
public class NullVirusScanner : IVirusScanner
{
    public bool IsEnabled => false;

    public Task<bool> IsReachableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default) =>
        Task.FromResult(new VirusScanResult(VirusScanOutcome.Clean));
}
