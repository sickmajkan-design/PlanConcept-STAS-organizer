namespace Construction.Application.Common.Interfaces;

/// <summary>Who said it. Tool results travel separately, not as a role.</summary>
public enum AssistantRole
{
    User = 1,
    Assistant = 2,
}

/// <summary>
/// One earlier turn, replayed as plain text.
/// </summary>
/// <remarks>
/// Deliberately text and nothing else. The panel holds the conversation, so
/// what comes back over HTTP is whatever it chose to send, and anything richer
/// — tool-use ids, thinking signatures — would be a claim from the client about
/// what the model previously did. Within a single request the transport keeps
/// the real blocks; across requests only the words survive.
/// </remarks>
public sealed record AssistantMessage(AssistantRole Role, string Text);

/// <summary>One tool as the model is shown it.</summary>
public sealed record AssistantToolDescriptor(
    string Name,
    string Description,
    string InputSchemaJson);

/// <summary>The model asking for a tool to be run.</summary>
public sealed record AssistantToolCall(string Id, string Name, string ArgumentsJson);

/// <summary>What running it produced. <paramref name="IsError"/> is reported to the model, not thrown.</summary>
public sealed record AssistantToolOutcome(string Id, string Content, bool IsError);

public sealed record AssistantUsage(int InputTokens, int OutputTokens)
{
    public static readonly AssistantUsage None = new(0, 0);

    public static AssistantUsage operator +(AssistantUsage left, AssistantUsage right) =>
        new(left.InputTokens + right.InputTokens, left.OutputTokens + right.OutputTokens);
}

/// <summary>The result of one call to the model.</summary>
public sealed record AssistantTurn(
    string Text,
    IReadOnlyList<AssistantToolCall> ToolCalls,
    AssistantUsage Usage);

public sealed record AssistantSessionOptions(
    string SystemPrompt,
    IReadOnlyList<AssistantToolDescriptor> Tools,
    IReadOnlyList<AssistantMessage> History);

/// <summary>
/// One conversation, alive for the length of one HTTP request.
/// </summary>
/// <remarks>
/// The session — not the caller — owns the running message list, because
/// continuing a tool-use exchange means echoing the model's own content blocks
/// back verbatim, ids and all. Flattening those to text between calls would
/// break the exchange, so they never leave the transport.
/// </remarks>
public interface IAssistantSession
{
    Task<AssistantTurn> SendAsync(string userText, CancellationToken cancellationToken = default);

    Task<AssistantTurn> ProvideToolResultsAsync(
        IReadOnlyList<AssistantToolOutcome> outcomes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Transport port for the assistant (implemented against the Anthropic API in
/// Infrastructure), in the same shape as <see cref="IPushSender"/> and
/// <see cref="IEmailSender"/>.
/// </summary>
public interface IAssistantClient
{
    /// <summary>False when no API key is configured; the feature is optional.</summary>
    bool IsConfigured { get; }

    IAssistantSession Start(AssistantSessionOptions options);
}
