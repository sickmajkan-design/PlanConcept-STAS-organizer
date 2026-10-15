using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Infrastructure.Antivirus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Construction.UnitTests.Antivirus;

/// <summary>
/// The scanner speaks ClamAV's INSTREAM protocol, so it is tested against a small stand-in daemon that
/// does the same: reads the command, reads length-prefixed chunks until a zero length, answers one line.
/// </summary>
public class ClamAvScannerTests
{
    /// <summary>A stand-in for clamd on a free local port.</summary>
    private sealed class FakeDaemon : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly Task _serving;

        public FakeDaemon(string reply)
        {
            _listener.Start();
            _serving = Task.Run(() => ServeAsync(reply));
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public string? Command { get; private set; }

        public byte[] Received { get; private set; } = [];

        public List<int> ChunkSizes { get; } = [];

        private async Task ServeAsync(string reply)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                await using var stream = client.GetStream();

                // The command: up to the NUL.
                var command = new List<byte>();
                var one = new byte[1];

                while (await stream.ReadAsync(one) == 1 && one[0] != 0)
                {
                    command.Add(one[0]);
                }

                Command = Encoding.ASCII.GetString(command.ToArray());

                var body = new MemoryStream();
                var length = new byte[4];

                while (true)
                {
                    await ReadExactlyAsync(stream, length);
                    var size = BinaryPrimitives.ReadInt32BigEndian(length);

                    if (size == 0)
                    {
                        break;
                    }

                    ChunkSizes.Add(size);
                    var chunk = new byte[size];
                    await ReadExactlyAsync(stream, chunk);
                    body.Write(chunk);
                }

                Received = body.ToArray();

                await stream.WriteAsync(Encoding.ASCII.GetBytes(reply + "\0"));
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer)
        {
            var read = 0;

            while (read < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read));

                if (n == 0)
                {
                    throw new EndOfStreamException();
                }

                read += n;
            }
        }

        public async Task WaitAsync() => await _serving.WaitAsync(TimeSpan.FromSeconds(5));

        public void Dispose() => _listener.Stop();
    }

    private static ClamAvScanner Scanner(int port, bool enabled = true, bool failOpen = false) =>
        new(
            Options.Create(new AntivirusSettings
            {
                Enabled = enabled,
                Host = "127.0.0.1",
                Port = port,
                TimeoutSeconds = 5,
                FailOpen = failOpen
            }),
            NullLogger<ClamAvScanner>.Instance);

    private static MemoryStream Bytes(int size)
    {
        var data = new byte[size];
        new Random(7).NextBytes(data);

        return new MemoryStream(data);
    }

    [Fact]
    public async Task A_clean_file_is_streamed_in_chunks_and_the_stream_is_left_where_it_was()
    {
        using var daemon = new FakeDaemon("stream: OK");
        var content = Bytes(150_000);
        content.Position = 0;
        var original = content.ToArray();

        var result = await Scanner(daemon.Port).ScanAsync(content);
        await daemon.WaitAsync();

        Assert.Equal(VirusScanOutcome.Clean, result.Outcome);
        Assert.Equal("zINSTREAM", daemon.Command);
        Assert.Equal(original, daemon.Received);
        Assert.True(daemon.ChunkSizes.Count >= 3, "a file larger than one chunk goes in several");
        Assert.All(daemon.ChunkSizes, size => Assert.InRange(size, 1, 64 * 1024));
        Assert.Equal(0, content.Position);
    }

    [Fact]
    public async Task A_recognised_file_is_reported_with_the_signature_name()
    {
        using var daemon = new FakeDaemon("stream: Win.Test.EICAR_HDB-1 FOUND");

        var result = await Scanner(daemon.Port).ScanAsync(Bytes(100));

        Assert.Equal(VirusScanOutcome.Infected, result.Outcome);
        Assert.Equal("Win.Test.EICAR_HDB-1", result.Signature);
    }

    [Theory]
    [InlineData("INSTREAM size limit exceeded. ERROR")]
    [InlineData("something unexpected")]
    public async Task An_answer_that_is_neither_ok_nor_found_is_not_a_pass(string reply)
    {
        using var daemon = new FakeDaemon(reply);

        var result = await Scanner(daemon.Port).ScanAsync(Bytes(100));

        Assert.Equal(VirusScanOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public async Task A_scanner_that_cannot_be_reached_refuses_by_default()
    {
        // A port nothing listens on.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var result = await Scanner(port).ScanAsync(Bytes(100));

        Assert.Equal(VirusScanOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public async Task Fail_open_lets_a_file_through_when_the_scanner_is_down()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var result = await Scanner(port, failOpen: true).ScanAsync(Bytes(100));

        Assert.Equal(VirusScanOutcome.Clean, result.Outcome);
    }

    [Fact]
    public async Task A_disabled_scanner_passes_everything_without_connecting()
    {
        var scanner = Scanner(port: 1, enabled: false);

        Assert.False(scanner.IsEnabled);
        Assert.Equal(VirusScanOutcome.Clean, (await scanner.ScanAsync(Bytes(10))).Outcome);
    }

    [Fact]
    public async Task The_guard_refuses_what_is_infected_and_what_could_not_be_checked()
    {
        using var infected = new FakeDaemon("stream: Eicar-Test-Signature FOUND");
        await Assert.ThrowsAsync<ConflictException>(() =>
            Scanner(infected.Port).EnsureCleanAsync(Bytes(10), CancellationToken.None));

        var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        var port = ((IPEndPoint)closed.LocalEndpoint).Port;
        closed.Stop();

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            Scanner(port).EnsureCleanAsync(Bytes(10), CancellationToken.None));

        // Switched off: nothing is checked and nothing is thrown.
        await Scanner(1, enabled: false).EnsureCleanAsync(Bytes(10), CancellationToken.None);
    }
}
