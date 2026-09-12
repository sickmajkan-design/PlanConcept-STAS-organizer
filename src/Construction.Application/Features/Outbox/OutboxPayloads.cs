using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Outbox;

/// <summary>
/// An email, as it sits in the queue. <see cref="Attachment"/>'s bytes travel
/// inside the same JSON payload as everything else here — fine for a
/// spreadsheet-sized file, and it means an attachment survives a retry
/// exactly like the rest of the message, with nothing extra to clean up.
/// </summary>
public record EmailPayload(
    string To,
    string Subject,
    string HtmlBody,
    EmailAttachment? Attachment = null);

/// <summary>
/// A push, as it sits in the queue.
/// </summary>
/// <remarks>
/// Recipients rather than device tokens. Tokens are resolved when the message
/// is sent, so a phone registered in the meantime still gets it and one that
/// was pruned in the meantime is not tried again — neither of which is true of
/// a token list frozen at enqueue time, which on a retry an hour later could
/// be a list of devices that no longer exist.
/// </remarks>
public record PushPayload(
    IReadOnlyList<Guid> UserIds,
    NotificationType Type,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data);
