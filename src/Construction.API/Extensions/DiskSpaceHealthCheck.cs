using Construction.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Construction.API.Extensions;

/// <summary>
/// Free space on the disk with the uploaded files and the database: Degraded when it is low, Unhealthy when it is close
/// to out, so an uptime monitor on <c>/health/dependencies</c> can raise the alarm before uploads start failing.
/// </summary>
/// <remarks>
/// Not part of readiness: a full disk is fixed by cleaning up, and taking the API out of rotation would not help.
/// </remarks>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly IDiskSpace _disk;

    public DiskSpaceHealthCheck(IDiskSpace disk)
    {
        _disk = disk;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_disk.Read() is not { } reading)
        {
            return Task.FromResult(HealthCheckResult.Healthy("The disk could not be read."));
        }

        var data = new Dictionary<string, object>
        {
            ["freeBytes"] = reading.FreeBytes,
            ["totalBytes"] = reading.TotalBytes,
        };

        var message = $"Disk space: {reading.Describe()}.";

        return Task.FromResult(reading.Level switch
        {
            DiskSpaceLevel.Critical => HealthCheckResult.Unhealthy(message, data: data),
            DiskSpaceLevel.Low => HealthCheckResult.Degraded(message, data: data),
            _ => HealthCheckResult.Healthy(message, data),
        });
    }
}
