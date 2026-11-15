using Construction.Application.Features.SystemStatus;
using Microsoft.Extensions.Configuration;

namespace Construction.Infrastructure.Monitoring;

/// <summary>Reads <c>ServerAlerts:ExtraEmails</c>, a comma or semicolon separated list, from the server's settings.</summary>
public sealed class ServerAlertRecipients : IServerAlertRecipients
{
    public ServerAlertRecipients(IConfiguration configuration)
    {
        ExtraEmails = (configuration["ServerAlerts:ExtraEmails"] ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(address => address.Contains('@'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> ExtraEmails { get; }
}
