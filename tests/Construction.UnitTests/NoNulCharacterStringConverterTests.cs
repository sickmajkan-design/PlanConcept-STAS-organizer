using System.Text.Json;
using Construction.API.Json;

namespace Construction.UnitTests;

/// <summary>
/// A NUL character in a JSON string used to reach Npgsql and answer 500 (a `text` column cannot
/// hold one). The converter turns it into an ordinary 400 instead, the same way malformed JSON
/// already is.
/// </summary>
public class NoNulCharacterStringConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new NoNulCharacterStringConverter() },
    };

    private sealed record Body(string? Name);

    [Fact]
    public void A_string_with_a_NUL_character_fails_to_parse()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Body>("{\"Name\":\"a\\u0000b\"}", Options));
    }

    [Fact]
    public void An_ordinary_string_still_parses()
    {
        var body = JsonSerializer.Deserialize<Body>("{\"Name\":\"Ana\"}", Options);

        Assert.Equal("Ana", body!.Name);
    }

    [Fact]
    public void A_null_value_still_parses()
    {
        var body = JsonSerializer.Deserialize<Body>("{\"Name\":null}", Options);

        Assert.Null(body!.Name);
    }

    [Fact]
    public void Unicode_that_is_not_NUL_still_parses()
    {
        var body = JsonSerializer.Deserialize<Body>("{\"Name\":\"\\u0107irilica \\u0402\"}", Options);

        Assert.Equal("ćirilica Ђ", body!.Name);
    }
}
