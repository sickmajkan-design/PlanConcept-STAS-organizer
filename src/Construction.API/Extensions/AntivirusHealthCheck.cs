using Construction.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Construction.API.Extensions;

/// <summary>
/// Whether the virus scanner answers. Healthy when scanning is not switched on at all.
/// </summary>
/// <remarks>
/// Not part of readiness: an API that cannot scan can still serve everything except sending files, and taking
/// it out of rotation would turn a partial outage into a total one. It has its own probe,
/// <c>/health/dependencies</c>, for an uptime monitor to watch, because the scanner's container stays "up" after
/// the process inside it has been killed.
/// </remarks>
public class AntivirusHealthCheck : IHealthCheck
{
    private readonly IVirusScanner _scanner;

    public AntivirusHealthCheck(IVirusScanner scanner)
    {
        _scanner = scanner;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_scanner.IsEnabled || await _scanner.IsReachableAsync(cancellationToken))
        {
            return HealthCheckResult.Healthy();
        }

        return HealthCheckResult.Unhealthy("The virus scanner does not answer.");
    }
}
