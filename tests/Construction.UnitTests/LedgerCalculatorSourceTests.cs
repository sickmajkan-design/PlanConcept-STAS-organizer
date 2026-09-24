using Construction.Application.Features.Ledgers.Models;

namespace Construction.UnitTests;

/// <summary>
/// A column can read a figure the system already knows (hours from the
/// timesheet, an hourly rate). Typing into it means different things depending
/// on whether the row has an automatic figure to disagree with.
/// </summary>
public class LedgerCalculatorSourceTests
{
    private static readonly Guid Hours = Guid.NewGuid();
    private static readonly Guid Pay = Guid.NewGuid();
    private static readonly Guid Rate = Guid.NewGuid();

    private static LedgerCalculator Calculator()
    {
        var source = new LedgerFormulaSource(
            LedgerSourceKinds.TimeEntryHours, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 6));

        return new LedgerCalculator(
        [
            (Rate, null),
            (Hours, new LedgerFormula([], [], source)),
            (Pay, new LedgerFormula([Rate, Hours], [])),
        ]);
    }

    private static Dictionary<Guid, string?> Stored(string? hours = null, string? rate = "20") => new()
    {
        [Hours] = hours,
        [Rate] = rate,
    };

    [Fact]
    public void A_sourced_column_shows_the_automatic_figure_and_feeds_the_formulas_that_use_it()
    {
        var row = Calculator().ComputeRow(Stored(), new Dictionary<Guid, decimal> { [Hours] = 8m });

        Assert.Equal(8m, row[Hours].Value);
        Assert.False(row[Hours].IsTyped);
        Assert.Equal(160m, row[Pay].Value);
    }

    [Fact]
    public void A_row_with_nothing_automatic_reads_zero()
    {
        var row = Calculator().ComputeRow(Stored());

        Assert.Equal(0m, row[Hours].Value);
        Assert.Equal(0m, row[Pay].Value);
    }

    [Fact]
    public void Typing_over_an_automatic_figure_is_an_override()
    {
        var row = Calculator().ComputeRow(Stored(hours: "12"), new Dictionary<Guid, decimal> { [Hours] = 8m });

        Assert.Equal(12m, row[Hours].Value);
        Assert.True(row[Hours].IsOverride);
        Assert.True(row[Hours].IsTyped);
        Assert.Equal(240m, row[Pay].Value);
    }

    [Fact]
    public void Typing_where_the_row_has_no_automatic_figure_is_ordinary_input()
    {
        var row = Calculator().ComputeRow(Stored(hours: "40"));

        Assert.Equal(40m, row[Hours].Value);
        Assert.False(row[Hours].IsOverride);
        Assert.True(row[Hours].IsTyped);
    }

    [Fact]
    public void A_zero_automatic_figure_still_counts_as_one_so_typing_over_it_is_an_override()
    {
        // The row is linked to a person and a project; they simply have no approved
        // hours yet. That is different from a row with no link at all.
        var row = Calculator().ComputeRow(Stored(hours: "40"), new Dictionary<Guid, decimal> { [Hours] = 0m });

        Assert.True(row[Hours].IsOverride);
    }

    [Fact]
    public void A_source_survives_a_round_trip_and_a_remap()
    {
        var source = new LedgerFormulaSource(
            LedgerSourceKinds.EmployeeHourlyRate, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var reread = LedgerFormula.Parse(new LedgerFormula([], [], source).ToJson());

        Assert.Equal(source, reread!.Source);
        Assert.Equal(source, reread.Remap(new Dictionary<Guid, Guid>()).Source);
    }
}
