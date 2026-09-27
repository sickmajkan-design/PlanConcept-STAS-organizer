using System.Text.Json;
using System.Text.Json.Serialization;

namespace Construction.API.Json;

/// <summary>
/// Refuses a JSON string that contains a NUL character (U+0000).
/// </summary>
/// <remarks>
/// PostgreSQL's <c>text</c>/<c>varchar</c> columns cannot hold a NUL byte — Npgsql throws, and
/// without this the request answers 500 instead of a plain 400, on any field a caller can put one
/// in (a name, a note, a device token). Thrown as <see cref="JsonException"/> so it is handled the
/// same way the framework already handles malformed JSON: a 400, not an unhandled exception.
/// </remarks>
public sealed class NoNulCharacterStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (value is not null && value.Contains('\0'))
        {
            throw new JsonException("Text cannot contain a NUL character.");
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
