using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Outbox;

/// <summary>
/// Writes queued messages into the caller's own unit of work.
/// </summary>
/// <remarks>
/// Lives in the Application layer rather than Infrastructure because there is
/// nothing infrastructural about it: it serialises a record and adds it to a
/// <c>DbSet</c>. The part that talks to PostgreSQL — claiming under
/// concurrency — is a separate seam.
/// </remarks>
public class OutboxWriter : IOutbox
{
    /// <summary>
    /// Shared with the reader, so a message written by one is understood by
    /// the other. Left at the default naming on purpose: the JSON is only ever
    /// read back by this application, and a message enqueued before a
    /// deployment is deserialised after it.
    /// </summary>
    internal static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxWriter(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public void Enqueue(EmailPayload email) => Add(OutboxMessageType.Email, email);

    public void Enqueue(PushPayload push) => Add(OutboxMessageType.Push, push);

    private void Add<T>(OutboxMessageType type, T payload)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        _context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload, SerializerOptions),
            CreatedAt = utcNow,
            // Due immediately. The processor runs on a short interval, so
            // "immediately" is a few seconds — which is the difference between
            // a request that waits for SMTP and one that does not.
            NextAttemptAt = utcNow,
        });
    }
}
