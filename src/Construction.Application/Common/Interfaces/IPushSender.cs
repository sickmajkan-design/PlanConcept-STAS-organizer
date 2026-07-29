namespace Construction.Application.Common.Interfaces;

public record PushSendResult(int SuccessCount, int FailureCount, IReadOnlyList<string> InvalidTokens)
{
    public static readonly PushSendResult Empty = new(0, 0, Array.Empty<string>());
}

/// <summary>
/// Transport port for push delivery (implemented with Firebase Cloud
/// Messaging in Infrastructure). Reports permanently invalid device tokens
/// so the caller can prune them.
/// </summary>
public interface IPushSender
{
    Task<PushSendResult> SendAsync(
        IReadOnlyList<string> deviceTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default);
}
