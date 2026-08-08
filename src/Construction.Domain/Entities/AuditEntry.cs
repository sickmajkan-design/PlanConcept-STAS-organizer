using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One recorded change to an audited entity: who, what, when, and the before
/// and after of every field that moved.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not a <c>BaseEntity</c>. An audit row is never updated and
/// never soft-deleted — the whole point is that it is append-only — so
/// <c>UpdatedAt</c>, <c>IsDeleted</c> and the interceptors that maintain them
/// would be meaningless here, and a row that could be edited would be worth
/// very little in the dispute it exists for.
/// </para>
/// <para>
/// The actor's email and role are copied in rather than joined to. The
/// account may be deleted, renamed, or promoted later, and the trail has to
/// say who did it at the time — not who they are now, and not a dangling id
/// once the account is gone.
/// </para>
/// </remarks>
public class AuditEntry
{
    /// <summary>
    /// A long rather than a Guid: this is the highest-volume table nobody
    /// queries by primary key, and it is always read in time order.
    /// </summary>
    public long Id { get; set; }

    public DateTime OccurredAt { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>The CLR type name, e.g. <c>Employee</c>.</summary>
    public string EntityName { get; set; } = null!;

    public Guid EntityId { get; set; }

    /// <summary>Null when the change came from a background job rather than a person.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The actor's address as it was at the time.</summary>
    public string? UserEmail { get; set; }

    /// <summary>The actor's role as it was at the time.</summary>
    public UserRole? UserRole { get; set; }

    /// <summary>Where the request came from, when there was one.</summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// The changed properties, as <c>{"Status":{"from":"Active","to":"OnLeave"}}</c>.
    /// </summary>
    /// <remarks>
    /// jsonb rather than a child table of one row per property. The trail is
    /// read as "what happened here", never as "every change to this one
    /// column", so a join would cost on every read to serve a query nobody
    /// makes — and jsonb still allows one if that turns out to be wrong.
    /// </remarks>
    public string ChangesJson { get; set; } = "{}";
}
