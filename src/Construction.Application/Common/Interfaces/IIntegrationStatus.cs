namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Whether the outside services the platform leans on have been set up, for
/// the health part of the setup checklist. Says nothing about whether they are
/// reachable right now — only whether anybody configured them.
/// </summary>
public interface IIntegrationStatus
{
    /// <summary>An SMTP host is set, so password-reset and report mail can go out.</summary>
    bool EmailConfigured { get; }

    /// <summary>Firebase credentials are set, so the phone app can be sent notifications.</summary>
    bool PushConfigured { get; }
}
