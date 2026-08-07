using Construction.Application.Features.Outbox;

namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Queues something to send, without sending it.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately synchronous and deliberately not saving. Enqueuing only adds a
/// row to the caller's change tracker, so it commits with the caller's own
/// <c>SaveChangesAsync</c> and therefore with the work that caused it. That is
/// the whole point: a password-reset token and the email carrying it are one
/// transaction, and there is no window in which the token exists and the email
/// does not.
/// </para>
/// <para>
/// A handler that enqueues and then throws before saving sends nothing, which
/// is correct — the thing the message was about did not happen either.
/// </para>
/// </remarks>
public interface IOutbox
{
    void Enqueue(EmailPayload email);

    void Enqueue(PushPayload push);
}
