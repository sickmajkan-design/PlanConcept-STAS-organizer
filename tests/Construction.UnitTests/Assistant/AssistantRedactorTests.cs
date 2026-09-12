using System.Text.Json;
using Construction.API.Assistant;

namespace Construction.UnitTests.Assistant;

/// <summary>
/// What must never reach the model, whatever a DTO happens to carry.
/// </summary>
/// <remarks>
/// The owner's ceiling for this feature is "no location history and no pay".
/// Keeping the Locations queries out of the tool registry does not achieve
/// that on its own — <c>TimeEntryDto</c> already carries the coordinates a
/// shift was stamped with, and <c>EmployeeDetailDto</c> carries what the
/// period cost. Both belong to tools the office genuinely needs, so the
/// ceiling has to hold at the field rather than at the query.
/// </remarks>
public class AssistantRedactorTests
{
    private sealed record Shift(
        Guid Id,
        string EmployeeName,
        double? StartLatitude,
        double? StartLongitude,
        double? EndLatitude,
        double? EndLongitude,
        int TotalMinutes);

    private sealed record Person(string FullName, decimal? TotalPay, string? Address, DateOnly? DateOfBirth);

    private sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount);

    private sealed record Site(string Name, Person Manager);

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void A_shift_keeps_its_hours_and_loses_its_position()
    {
        var json = Parse(AssistantRedactor.Serialise(new Shift(
            Guid.NewGuid(),
            "Marko Marković",
            44.81,
            20.46,
            44.82,
            20.47,
            465)));

        Assert.Equal("Marko Marković", json.GetProperty("employeeName").GetString());
        Assert.Equal(465, json.GetProperty("totalMinutes").GetInt32());

        // The answer the office wants — how long somebody worked — survives
        // intact. Where they were standing does not.
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("startLatitude").GetString());
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("startLongitude").GetString());
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("endLatitude").GetString());
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("endLongitude").GetString());
    }

    [Fact]
    public void A_person_keeps_their_name_and_loses_their_pay_address_and_birthday()
    {
        var json = Parse(AssistantRedactor.Serialise(
            new Person("Ana Anić", 184_500m, "Kneza Miloša 12", new DateOnly(1988, 4, 3))));

        Assert.Equal("Ana Anić", json.GetProperty("fullName").GetString());
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("totalPay").GetString());
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("address").GetString());
        Assert.Equal(AssistantRedactor.Placeholder, json.GetProperty("dateOfBirth").GetString());
    }

    [Fact]
    public void Withholding_reaches_inside_a_list()
    {
        // Every tool that lists anything returns a page, so a redactor that
        // only walked the top level would cover almost nothing.
        var json = Parse(AssistantRedactor.Serialise(new Page<Shift>(
            [new Shift(Guid.NewGuid(), "Marko", 44.81, 20.46, null, null, 60)],
            1)));

        var row = json.GetProperty("items")[0];

        Assert.Equal(AssistantRedactor.Placeholder, row.GetProperty("startLatitude").GetString());
        Assert.Equal(60, row.GetProperty("totalMinutes").GetInt32());
    }

    [Fact]
    public void Withholding_reaches_a_nested_object()
    {
        var json = Parse(AssistantRedactor.Serialise(
            new Site("Vidikovac", new Person("Ana", 1m, "Somewhere", null))));

        Assert.Equal(
            AssistantRedactor.Placeholder,
            json.GetProperty("manager").GetProperty("totalPay").GetString());
    }

    [Fact]
    public void A_shift_with_no_position_carries_no_coordinate_at_all()
    {
        // Work in a basement records no fix, and serialisation drops nulls, so
        // the property is simply absent rather than present-and-withheld. Both
        // outcomes are correct here; this pins that the empty case does not
        // somehow reintroduce the field.
        var json = Parse(AssistantRedactor.Serialise(
            new Shift(Guid.NewGuid(), "Marko", null, null, null, null, 30)));

        Assert.False(
            json.TryGetProperty("startLatitude", out _),
            "a null coordinate is dropped by serialisation, which is fine — nothing leaks");
        Assert.Equal(30, json.GetProperty("totalMinutes").GetInt32());
    }

    [Theory]
    [InlineData("startLatitude")]
    [InlineData("EndLongitude")]
    [InlineData("latitude")]
    [InlineData("totalPay")]
    [InlineData("hourlyRate")]
    [InlineData("labourCost")]
    [InlineData("employeeHourlyRate")]
    [InlineData("homeAddress")]
    [InlineData("dateOfBirth")]
    public void These_names_are_withheld(string property)
    {
        Assert.True(AssistantRedactor.IsWithheld(property));
    }

    [Theory]
    [InlineData("fullName")]
    [InlineData("totalMinutes")]
    [InlineData("approvedMinutes")]
    [InlineData("status")]
    [InlineData("projectName")]
    [InlineData("quantity")]
    [InlineData("registration")]
    public void And_these_ordinary_ones_are_not(string property)
    {
        // A redactor that swallowed the answer would be worse than none: the
        // office would get "[withheld]" where it asked for hours.
        Assert.False(AssistantRedactor.IsWithheld(property));
    }

    [Fact]
    public void A_null_result_is_json_null_rather_than_a_crash()
    {
        Assert.Equal("null", AssistantRedactor.Serialise(null));
    }
}
