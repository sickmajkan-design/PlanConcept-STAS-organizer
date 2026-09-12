namespace Construction.Infrastructure.Ai;

/// <summary>
/// What the assistant needs to talk to Claude.
/// </summary>
/// <remarks>
/// Shaped like <see cref="Notifications.FirebaseSettings"/> rather than
/// <c>JwtSettings</c>: the key is optional, and an installation without one
/// runs perfectly well with the assistant switched off. Requiring it at
/// startup would stop a deployment that never wanted the feature.
/// </remarks>
public class AnthropicSettings
{
    public const string SectionName = "Anthropic";

    /// <summary>Never in a committed file — supplied as <c>Anthropic__ApiKey</c>.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5";

    /// <summary>
    /// Ceiling on one answer. The office asks for a number or a short list, so
    /// this is a cost guard rather than a limit anybody meets.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>
    /// Cut-off for the whole exchange, tool round-trips included. Kept under
    /// the panel's own timeout so the caller is told what happened rather than
    /// left with a dead request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 55;

    /// <summary>Messages one user may send in <see cref="RateLimitWindowSeconds"/>.</summary>
    public int RateLimitPermitCount { get; set; } = 20;

    public int RateLimitWindowSeconds { get; set; } = 300;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
