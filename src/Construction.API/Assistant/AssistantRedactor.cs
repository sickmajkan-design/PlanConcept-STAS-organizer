using System.Text.Json;
using System.Text.Json.Nodes;

namespace Construction.API.Assistant;

/// <summary>
/// Strips fields that must not leave the server, whatever a DTO happens to carry.
/// </summary>
/// <remarks>
/// <para>
/// The tool registry is an allow-list, which decides <i>which records</i> the
/// assistant may read. This decides <i>which fields</i> travel, and it exists
/// because the two are not the same question. Leaving the Locations feature out
/// of the registry does not keep GPS off the wire: <c>TimeEntryDto</c> carries
/// <c>StartLatitude</c>/<c>StartLongitude</c>, and <c>EmployeeDetailDto</c>
/// carries <c>TotalPay</c>. Both are tools the office genuinely needs.
/// </para>
/// <para>
/// So it works by field name rather than by type. A column added to an
/// existing DTO in six months is covered without anyone remembering this file
/// exists — which is the only kind of guard that survives.
/// </para>
/// </remarks>
public static class AssistantRedactor
{
    /// <summary>What is put in place of a removed value, so its absence is visible.</summary>
    public const string Placeholder = "[withheld]";

    /// <summary>
    /// A field is withheld when its name ends with one of these, compared
    /// case-insensitively. Suffixes rather than exact names so that
    /// <c>StartLatitude</c>, <c>EndLatitude</c> and a future
    /// <c>ReportedLatitude</c> are all caught by the one entry.
    /// </summary>
    public static readonly IReadOnlyList<string> WithheldSuffixes =
    [
        // Location. The owner's decision, and audit finding C7 — the lawful
        // basis for tracking is still not on record, so a movement trail is
        // the last thing that should reach a third party.
        "latitude",
        "longitude",

        // Wages. "What Marko earns" is not a question the office assistant
        // answers, and a figure that leaked here would be repeated in a chat
        // panel that anyone with a Foreman account can open.
        "totalpay",
        "hourlyrate",
        "dailyrate",
        "labourcost",
        "laborcost",
        "payrate",

        // Not part of the stated ceiling, and withheld anyway: a home address
        // and a date of birth answer no question about a building site, and
        // sending them to a third party for no purpose is the kind of thing a
        // DPIA asks about. Removing this pair costs nothing.
        "dateofbirth",
        "address",
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serialises a tool result with every withheld field removed.</summary>
    public static string Serialise(object? result)
    {
        if (result is null)
        {
            return "null";
        }

        var node = JsonSerializer.SerializeToNode(result, result.GetType(), SerializerOptions);

        Redact(node);

        return node?.ToJsonString() ?? "null";
    }

    /// <summary>True when a property of this name must not be sent.</summary>
    public static bool IsWithheld(string propertyName)
    {
        var name = propertyName.ToLowerInvariant();

        foreach (var suffix in WithheldSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject o:
                // Collected first: replacing while enumerating an object
                // invalidates the enumerator.
                var withheld = o
                    .Where(property => IsWithheld(property.Key))
                    .Select(property => property.Key)
                    .ToList();

                foreach (var name in withheld)
                {
                    o[name] = Placeholder;
                }

                foreach (var property in o)
                {
                    if (!IsWithheld(property.Key))
                    {
                        Redact(property.Value);
                    }
                }

                break;

            case JsonArray a:
                foreach (var item in a)
                {
                    Redact(item);
                }

                break;
        }
    }
}
