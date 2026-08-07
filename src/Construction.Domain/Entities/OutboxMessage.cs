using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Something to send, written down before anyone tries to send it.
/// </summary>
/// <remarks>
/// <para>
/// The row is inserted in the same transaction as the work that caused it, so
/// a password-reset token and the email carrying it either both exist or
/// neither does. Sending from inside the request could not offer that: the
/// token would be committed and the email lost whenever SMTP was down, and the
/// person would be left waiting for a link nobody was ever going to send.
/// </para>
/// <para>
/// The payload is stored as JSON rather than as columns because the two kinds
/// share nothing — an email has an address and a body, a push has recipients
/// and a title — and a table with both sets of columns half-null is a table
/// that has to be read with a rule in mind.
/// </para>
/// </remarks>
public class OutboxMessage
{
    public Guid Id { get; set; }

    public OutboxMessageType Type { get; set; }

    /// <summary>The message itself, shaped by <see cref="Type"/>.</summary>
    public string PayloadJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this message may next be picked up.
    /// </summary>
    /// <remarks>
    /// Doubles as the lease. Claiming pushes it forward, so a message being
    /// worked on is not due, and a worker that dies mid-send leaves a message
    /// that becomes due again by itself rather than one that is stuck.
    /// </remarks>
    public DateTime NextAttemptAt { get; set; }

    public int Attempts { get; set; }

    /// <summary>Set by the claim, so a worker can find the rows it took.</summary>
    public Guid? ClaimId { get; set; }

    public DateTime? SentAt { get; set; }

    /// <summary>Set once the attempts run out. Nothing retries after this.</summary>
    public DateTime? AbandonedAt { get; set; }

    /// <summary>Why the last attempt failed, for whoever has to explain it.</summary>
    public string? LastError { get; set; }

    public bool IsPending => SentAt is null && AbandonedAt is null;
}
