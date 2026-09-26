using System.Text.Json;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>Where a column's figure comes from when it is not worked out from other columns.</summary>
public static class LedgerSourceKinds
{
    /// <summary>Approved hours the row's employee worked on the section's project, in the date range.</summary>
    public const string TimeEntryHours = "timeEntryHours";

    /// <summary>The row's employee's hourly rate in force at the end of the date range.</summary>
    public const string EmployeeHourlyRate = "employeeHourlyRate";

    /// <summary>Approved fuel expenses of the vehicles assigned to the row's employee, in the date range.</summary>
    public const string VehicleFuelCost = "vehicleFuelCost";

    /// <summary>The monthly rental of the vehicles assigned to the row's employee, for the days it applied.</summary>
    public const string VehicleRentalCost = "vehicleRentalCost";

    /// <summary>The row's employee's share of accommodation rent, as the accommodation pages work it out.</summary>
    public const string AccommodationCost = "accommodationCost";

    /// <summary>Approved refunds the row's employee is paid back with the payroll of the range's month.</summary>
    public const string EmployeeRefunds = "employeeRefunds";
}

/// <summary>A figure the system already knows, offered as the cell's value.</summary>
public sealed record LedgerFormulaSource(string Kind, DateOnly From, DateOnly To);

/// <summary>One column added to or taken from a formula's result.</summary>
public sealed record LedgerFormulaTerm(Guid ColumnId, int Sign);

/// <summary>
/// How a computed column is worked out from the other columns of the same row:
/// an optional product of two columns, plus a signed sum of others.
/// </summary>
/// <remarks>
/// <para>
/// That is every calculation the monthly payroll needs — <c>hours = week1 + … +
/// week5</c>, <c>pay = rate × hours − advance + bonus</c>, <c>margin = billed −
/// pay</c> — and deliberately nothing more. There is no division, no free
/// expression and no reference to another row, so a formula cannot fail to
/// evaluate, and the only way to build one is through a template that has been
/// tested.
/// </para>
/// <para>
/// A missing value counts as zero, as it does in a spreadsheet.
/// </para>
/// </remarks>
public sealed record LedgerFormula(
    IReadOnlyList<Guid> Product,
    IReadOnlyList<LedgerFormulaTerm> Terms,
    LedgerFormulaSource? Source = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static LedgerFormula Sum(params Guid[] columns) =>
        new([], columns.Select(c => new LedgerFormulaTerm(c, 1)).ToList());

    public IEnumerable<Guid> ReferencedColumns => Product.Concat(Terms.Select(t => t.ColumnId));

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>Reads what <see cref="ToJson"/> wrote. Null for an empty or unreadable value.</summary>
    public static LedgerFormula? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LedgerFormula>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The same formula with every column id replaced — used when a month is copied.</summary>
    public LedgerFormula Remap(IReadOnlyDictionary<Guid, Guid> map) => new(
        Product.Select(id => map.TryGetValue(id, out var to) ? to : id).ToList(),
        Terms.Select(t => new LedgerFormulaTerm(map.TryGetValue(t.ColumnId, out var to) ? to : t.ColumnId, t.Sign)).ToList(),
        Source);
}
