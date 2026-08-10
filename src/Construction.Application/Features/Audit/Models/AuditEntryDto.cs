using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Audit.Models;

/// <summary>One recorded change, as the panel shows it.</summary>
public class AuditEntryDto
{
    public long Id { get; init; }

    public DateTime OccurredAt { get; init; }

    public string Action { get; init; } = null!;

    public string EntityName { get; init; } = null!;

    public Guid EntityId { get; init; }

    public Guid? UserId { get; init; }

    /// <summary>Who, as they were at the time. Null for a background job.</summary>
    public string? UserEmail { get; init; }

    public string? UserRole { get; init; }

    public string? IpAddress { get; init; }

    /// <summary>
    /// The changed fields, keyed by property name.
    /// </summary>
    /// <remarks>
    /// Parsed out of the stored jsonb rather than passed through as a string,
    /// so the API emits a real object and the client is not left parsing JSON
    /// out of a JSON string.
    /// </remarks>
    public IReadOnlyDictionary<string, AuditChangeDto> Changes { get; init; } =
        new Dictionary<string, AuditChangeDto>();
}

public class AuditChangeDto
{
    [JsonPropertyName("from")]
    public string? From { get; init; }

    [JsonPropertyName("to")]
    public string? To { get; init; }
}

/// <summary>How an <see cref="AuditEntry"/> becomes an <see cref="AuditEntryDto"/>.</summary>
/// <remarks>
/// See <c>EmployeeMapping</c> for the convention these all follow. The one
/// thing this projection does that the others do not is call a method —
/// <see cref="Parse"/>. EF cannot translate that, and does not have to: a call
/// in the outermost <c>Select</c> is evaluated on the client, on rows already
/// fetched. It must stay in the outermost projection for that to hold.
/// </remarks>
public static class AuditEntryMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static readonly Expression<Func<AuditEntry, AuditEntryDto>> Projection = entry =>
        new AuditEntryDto
        {
            Id = entry.Id,
            OccurredAt = entry.OccurredAt,
            Action = entry.Action.ToString(),
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            UserId = entry.UserId,
            UserEmail = entry.UserEmail,
            UserRole = entry.UserRole == null ? null : entry.UserRole.ToString(),
            IpAddress = entry.IpAddress,
            Changes = Parse(entry.ChangesJson),
        };

    private static readonly Func<AuditEntry, AuditEntryDto> Compiled = Projection.Compile();

    public static AuditEntryDto ToDto(AuditEntry entry) => Compiled(entry);

    /// <summary>
    /// Never throws. A trail entry with unreadable JSON is still evidence that
    /// something happened, at a time, by somebody — and failing the whole page
    /// because one row is malformed would hide the other nine hundred.
    /// </summary>
    private static IReadOnlyDictionary<string, AuditChangeDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, AuditChangeDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, AuditChangeDto>>(json, Json)
                ?? new Dictionary<string, AuditChangeDto>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, AuditChangeDto>();
        }
    }
}
