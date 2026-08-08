using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Common;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Construction.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes an <see cref="AuditEntry"/> for every change to an
/// <see cref="IAuditable"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// An interceptor rather than something handlers call. A trail that depends on
/// being remembered is a trail with holes in it, and the holes are wherever
/// somebody was in a hurry — which correlates uncomfortably well with the
/// changes an investigation cares about. Here there is nothing to remember:
/// the rows are written from the change tracker, so a handler cannot modify an
/// audited entity without it being recorded.
/// </para>
/// <para>
/// <strong>Ordering matters, though not in the obvious way.</strong> This runs
/// after <c>SoftDeleteInterceptor</c>, which has already turned a delete into
/// a modification that sets <c>IsDeleted</c>. So a soft delete arrives here as
/// <c>EntityState.Modified</c> and is recognised by that flag having just been
/// set.
/// </para>
/// <para>
/// Reversing the registration would still produce
/// <see cref="AuditAction.Deleted"/> — the state would simply be
/// <c>EntityState.Deleted</c> instead, which the first branch below already
/// handles. What would change is the payload: a hard delete records the whole
/// row as <c>value → null</c>, being the last chance to say what was lost,
/// and a soft delete deliberately does not, because nothing was lost and the
/// row is still on disk under its <c>IsDeleted</c> flag. Reversed, every
/// soft delete would carry a full snapshot implying the data was destroyed.
/// Two tests hold the distinction in place, because asserting the action
/// alone did not: that mutation survived.
/// </para>
/// <para>
/// The audit rows join the caller's transaction: they are added to the same
/// change tracker and written by the same <c>SaveChanges</c>. A change that
/// rolls back leaves no trail of having happened, and a trail entry cannot
/// survive the change it describes.
/// </para>
/// </remarks>
public class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Never recorded, whatever the entity says.
    /// </summary>
    /// <remarks>
    /// The safety net described on <see cref="NotAuditedAttribute"/>. It is
    /// matched against the property name, case-insensitively, as a substring:
    /// <c>PasswordHash</c>, <c>TokenHash</c> and <c>ClientSecret</c> all fail
    /// it. Deliberately blunt — the cost of a false positive is a missing
    /// field in a report, and the cost of a false negative is a credential in
    /// a table that outlives the account.
    /// </remarks>
    private static readonly string[] NeverRecorded =
        ["password", "hash", "token", "secret"];

    /// <summary>
    /// Columns the trail already says another way.
    /// </summary>
    /// <remarks>
    /// The key is the row's <c>EntityId</c>, the timestamps are its
    /// <c>OccurredAt</c>, and the delete flags are its <c>Action</c>.
    /// Recording them again would put "UpdatedAt: null → 2026-08-08T…" on
    /// every single entry and "IsDeleted: false → true" on every deletion —
    /// bookkeeping the ORM did, sitting in the column where a reader is
    /// looking for what a person did.
    /// </remarks>
    private static readonly HashSet<string> Bookkeeping =
    [
        nameof(BaseEntity.Id),
        nameof(BaseEntity.CreatedAt),
        nameof(BaseEntity.UpdatedAt),
        nameof(ISoftDeletable.IsDeleted),
        nameof(ISoftDeletable.DeletedAt)
    ];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public AuditTrailInterceptor(
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Record(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Record(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var occurredAt = _dateTimeProvider.UtcNow;

        // Materialised before anything is added: adding audit rows mutates the
        // change tracker, and enumerating it while it grows would throw — or,
        // worse on a different EF version, start auditing the audit rows.
        var audited = context.ChangeTracker
            .Entries<IAuditable>()
            .Where(entry => entry.State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            .ToList();

        if (audited.Count == 0)
        {
            return;
        }

        var entries = new List<AuditEntry>();

        foreach (var entry in audited)
        {
            var action = DescribeAction(entry);
            var changes = Describe(entry, action);

            // A save that touched the row without changing a recordable value
            // — a timestamp refresh, or a property this trail does not keep.
            // A row saying "somebody changed nothing" is noise in the one
            // place noise is most expensive.
            //
            // Only updates are dropped. A creation or a deletion is worth
            // recording for having happened at all, and after the bookkeeping
            // columns are stripped a deletion usually has nothing else left in
            // it — dropping those would lose the event entirely.
            if (action == AuditAction.Updated && changes.Count == 0)
            {
                continue;
            }

            entries.Add(new AuditEntry
            {
                OccurredAt = occurredAt,
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = EntityIdOf(entry),
                UserId = _currentUserService.UserId,
                UserEmail = _currentUserService.Email,
                UserRole = _currentUserService.Role,
                IpAddress = _currentUserService.IpAddress,
                ChangesJson = JsonSerializer.Serialize(changes, Json)
            });
        }

        context.Set<AuditEntry>().AddRange(entries);
    }

    private static AuditAction DescribeAction(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditAction.Created;
        }

        // EntityState.Deleted only reaches here for an entity that is not
        // soft-deletable; for the rest the soft-delete interceptor has already
        // rewritten the state, and the flag below is what is left of the
        // intent.
        if (entry.State == EntityState.Deleted)
        {
            return AuditAction.Deleted;
        }

        var deletedFlag = entry.Properties.FirstOrDefault(
            p => p.Metadata.Name == nameof(ISoftDeletable.IsDeleted));

        return deletedFlag is { IsModified: true, CurrentValue: true }
            ? AuditAction.Deleted
            : AuditAction.Updated;
    }

    /// <summary>
    /// The before and after of each property worth recording.
    /// </summary>
    private static Dictionary<string, PropertyChange> Describe(
        EntityEntry entry,
        AuditAction action)
    {
        var changes = new Dictionary<string, PropertyChange>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (IsNotRecordable(property))
            {
                continue;
            }

            switch (action)
            {
                case AuditAction.Created:
                    // Everything the row started with, so the trail can
                    // reconstruct the record without the row still existing.
                    if (property.CurrentValue is not null)
                    {
                        changes[name] = new PropertyChange(null, Stringify(property.CurrentValue));
                    }

                    break;

                case AuditAction.Deleted when entry.State == EntityState.Deleted:
                    // A hard delete: what is about to be lost.
                    if (property.CurrentValue is not null)
                    {
                        changes[name] = new PropertyChange(Stringify(property.CurrentValue), null);
                    }

                    break;

                default:
                    if (!property.IsModified)
                    {
                        continue;
                    }

                    var before = Stringify(property.OriginalValue);
                    var after = Stringify(property.CurrentValue);

                    // EF marks a property modified when it has been assigned,
                    // not when the value differs. Handlers that write every
                    // field from a form would otherwise fill the trail with
                    // "Position: Foreman → Foreman".
                    if (before != after)
                    {
                        changes[name] = new PropertyChange(before, after);
                    }

                    break;
            }
        }

        return changes;
    }

    private static bool IsNotRecordable(PropertyEntry property)
    {
        var name = property.Metadata.Name;

        if (Bookkeeping.Contains(name))
        {
            return true;
        }

        if (NeverRecorded.Any(fragment =>
                name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return property.Metadata.PropertyInfo
            ?.IsDefined(typeof(NotAuditedAttribute), inherit: true) == true;
    }

    /// <summary>
    /// The primary key, which for every audited entity is a single Guid.
    /// </summary>
    /// <remarks>
    /// Read at this point rather than after the save because EF generates
    /// Guid keys on the client, so the value is already final for an insert.
    /// A test asserts that, since it is the assumption the whole trail hangs
    /// on: were the key database-generated, every created row would be
    /// recorded against an empty id.
    /// </remarks>
    private static Guid EntityIdOf(EntityEntry entry)
    {
        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());

        return key?.CurrentValue is Guid id ? id : Guid.Empty;
    }

    private static string? Stringify(object? value) => value switch
    {
        null => null,
        string text => text,
        DateTime utc => utc.ToString("O"),
        DateTimeOffset offset => offset.ToString("O"),
        DateOnly date => date.ToString("O"),
        TimeOnly time => time.ToString("O"),
        // Enums by name. The numeric value is meaningless to whoever reads
        // the trail, and renumbering the enum later would silently rewrite
        // the history's meaning.
        Enum flag => flag.ToString(),
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    /// <summary>One field's before and after.</summary>
    private sealed record PropertyChange(string? From, string? To);
}
