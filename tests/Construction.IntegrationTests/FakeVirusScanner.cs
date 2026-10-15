using System.Text;
using Construction.Application.Common.Interfaces;

namespace Construction.IntegrationTests;

/// <summary>
/// A scanner for tests: recognises the standard EICAR test string, and can be made unreachable.
/// </summary>
public class FakeVirusScanner : IVirusScanner
{
    /// <summary>The harmless string every antivirus product treats as a virus, for testing.</summary>
    public const string Eicar = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    public bool IsEnabled => true;

    /// <summary>When true the scanner answers that it cannot check anything.</summary>
    public bool Down { get; set; }

    public int Scans { get; private set; }

    public async Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        Scans++;

        if (Down)
        {
            return new VirusScanResult(VirusScanOutcome.Unavailable);
        }

        var start = content.Position;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        content.Position = start;

        var text = Encoding.Latin1.GetString(copy.ToArray());

        return text.Contains(Eicar, StringComparison.Ordinal)
            ? new VirusScanResult(VirusScanOutcome.Infected, "Eicar-Test-Signature")
            : new VirusScanResult(VirusScanOutcome.Clean);
    }
}
