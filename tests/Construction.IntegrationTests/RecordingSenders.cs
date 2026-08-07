using Construction.Application.Common.Interfaces;

namespace Construction.IntegrationTests;

/// <summary>
/// An email sender that records instead of sending, and fails when told to.
/// </summary>
/// <remarks>
/// Replaces Infrastructure's SMTP sender for the test run. The real one logs
/// instead of connecting when no host is configured, which is right for a
/// developer machine and useless here: it never fails, so the retry and
/// dead-letter paths would go unexercised. Registered as a singleton so a test
/// can look at what happened across the several scopes one sweep uses.
/// </remarks>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly Lock _gate = new();

    private readonly List<(string To, string Subject, string HtmlBody)> _sent = [];

    /// <summary>Set to make every send throw. Cleared between tests.</summary>
    public Exception? FailWith { get; set; }

    /// <summary>
    /// Runs while a send is in flight.
    /// </summary>
    /// <remarks>
    /// The hook that makes a race testable. A second worker starting while the
    /// first is mid-send is the interleaving the claim lease exists for, and
    /// it cannot be produced reliably by starting two sweeps and hoping.
    /// </remarks>
    public Func<Task>? OnSend { get; set; }

    public IReadOnlyList<(string To, string Subject, string HtmlBody)> Sent
    {
        get
        {
            lock (_gate)
            {
                return _sent.ToList();
            }
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _sent.Clear();
            FailWith = null;
            OnSend = null;
        }
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (FailWith is { } failure)
        {
            throw failure;
        }

        lock (_gate)
        {
            _sent.Add((to, subject, htmlBody));
        }

        if (OnSend is { } hook)
        {
            // Cleared first, so a hook that triggers another send does not
            // recurse for ever.
            OnSend = null;
            await hook();
        }
    }
}

/// <summary>A push sender that records instead of sending.</summary>
public sealed class RecordingPushSender : IPushSender
{
    private readonly Lock _gate = new();

    private readonly List<(IReadOnlyList<string> Tokens, string Title, string Body)> _sent = [];

    public Exception? FailWith { get; set; }

    /// <summary>Tokens the next send should report as permanently dead.</summary>
    public IReadOnlyList<string> InvalidTokens { get; set; } = [];

    public IReadOnlyList<(IReadOnlyList<string> Tokens, string Title, string Body)> Sent
    {
        get
        {
            lock (_gate)
            {
                return _sent.ToList();
            }
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _sent.Clear();
            FailWith = null;
            InvalidTokens = [];
        }
    }

    public Task<PushSendResult> SendAsync(
        IReadOnlyList<string> deviceTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        if (FailWith is { } failure)
        {
            return Task.FromException<PushSendResult>(failure);
        }

        lock (_gate)
        {
            _sent.Add((deviceTokens.ToList(), title, body));
        }

        return Task.FromResult(new PushSendResult(
            deviceTokens.Count - InvalidTokens.Count, InvalidTokens.Count, InvalidTokens));
    }
}
