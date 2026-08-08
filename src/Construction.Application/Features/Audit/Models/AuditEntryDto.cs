using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
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

public class AuditEntryMappingProfile : Profile
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public AuditEntryMappingProfile()
    {
        CreateMap<AuditEntry, AuditEntryDto>()
            .ForMember(d => d.Action, opt => opt.MapFrom(s => s.Action.ToString()))
            .ForMember(d => d.UserRole, opt => opt.MapFrom(s =>
                s.UserRole == null ? null : s.UserRole.ToString()))
            // Deserialised after the query rather than inside it: this cannot
            // be translated to SQL, so it runs on the materialised rows.
            .ForMember(d => d.Changes, opt => opt.MapFrom(s => Parse(s.ChangesJson)));
    }

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
