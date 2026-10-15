using Construction.Application.Common.Exceptions;

namespace Construction.Application.Common.Interfaces;

public enum VirusScanOutcome
{
    /// <summary>Nothing was found, or scanning is switched off.</summary>
    Clean = 1,

    /// <summary>The scanner recognised something.</summary>
    Infected = 2,

    /// <summary>The scanner could not answer, so nothing can be said about the file.</summary>
    Unavailable = 3
}

public record VirusScanResult(VirusScanOutcome Outcome, string? Signature = null);

/// <summary>Looks at the bytes of a file somebody is about to put on a record.</summary>
/// <remarks>
/// Uploads are the one place where a person outside the office hands this system arbitrary content that
/// is later opened by the office, so every path that stores an uploaded file goes through this first.
/// </remarks>
public interface IVirusScanner
{
    /// <summary>False when scanning is not configured; <see cref="ScanAsync"/> then returns clean.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Scans <paramref name="content"/> from its current position. The stream is left where it was, so
    /// the caller can go on and store it.
    /// </summary>
    Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}

public static class VirusScannerExtensions
{
    /// <summary>
    /// Refuses a file that is infected, and a file that could not be checked when checking is on.
    /// </summary>
    /// <remarks>
    /// "Could not be checked" is a 503, not a pass: a scanner that is down must not turn into an open
    /// door. The person is told to try again, which is true — it is worth making the same request later.
    /// </remarks>
    public static async Task EnsureCleanAsync(
        this IVirusScanner scanner,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!scanner.IsEnabled)
        {
            return;
        }

        var result = await scanner.ScanAsync(content, cancellationToken);

        switch (result.Outcome)
        {
            case VirusScanOutcome.Infected:
                throw new ConflictException("The file was refused: the virus scan found something in it.");

            case VirusScanOutcome.Unavailable:
                throw new ServiceUnavailableException(
                    "Files cannot be checked right now. Please try again in a few minutes.");
        }
    }
}
