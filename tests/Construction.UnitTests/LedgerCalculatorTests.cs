using Construction.Application.Features.Ledgers.Models;

namespace Construction.UnitTests;

public class LedgerCalculatorTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid Product = Guid.NewGuid();
    private static readonly Guid Total = Guid.NewGuid();

    private static LedgerCalculator Calculator() => new(
    [
        (A, null),
        (B, null),
        (Product, new LedgerFormula([A, B], [])),
        (Total, new LedgerFormula([], [new LedgerFormulaTerm(Product, 1), new LedgerFormulaTerm(A, -1)])),
    ]);

    private static Dictionary<Guid, string?> Stored(string? a, string? b, string? typedTotal = null) => new()
    {
        [A] = a,
        [B] = b,
        [Total] = typedTotal,
    };

    [Fact]
    public void A_product_and_a_signed_sum_are_worked_out_through_another_computed_column()
    {
        var row = Calculator().ComputeRow(Stored("4", "5"));

        Assert.Equal(20m, row[Product].Value);
        Assert.Equal(16m, row[Total].Value); // 4×5 − 4
    }

    [Fact]
    public void A_missing_or_unreadable_value_counts_as_zero()
    {
        var row = Calculator().ComputeRow(Stored(null, "abc"));

        Assert.Equal(0m, row[Product].Value);
        Assert.Equal(0m, row[Total].Value);
    }

    [Fact]
    public void Both_decimal_separators_are_read()
    {
        var row = Calculator().ComputeRow(Stored("2,5", "2.0"));

        Assert.Equal(5m, row[Product].Value);
    }

    [Fact]
    public void A_typed_value_wins_over_the_formula_and_is_reported_as_an_override()
    {
        var row = Calculator().ComputeRow(Stored("4", "5", typedTotal: "100"));

        Assert.Equal(100m, row[Total].Value);
        Assert.True(row[Total].IsOverride);
        Assert.False(row[Product].IsOverride);
    }

    [Fact]
    public void Ordinary_columns_are_not_computed()
    {
        var calculator = Calculator();

        Assert.False(calculator.IsComputed(A));
        Assert.True(calculator.IsComputed(Product));
    }

    [Fact]
    public void A_formula_that_depends_on_itself_is_detected_and_evaluates_to_zero_instead_of_hanging()
    {
        var x = Guid.NewGuid();
        var y = Guid.NewGuid();
        var loop = new LedgerCalculator(
        [
            (x, new LedgerFormula([], [new LedgerFormulaTerm(y, 1)])),
            (y, new LedgerFormula([], [new LedgerFormulaTerm(x, 1)])),
        ]);

        Assert.True(loop.HasCycle());
        Assert.Equal(0m, loop.ComputeRow(new Dictionary<Guid, string?>())[x].Value);
        Assert.False(Calculator().HasCycle());
    }

    [Fact]
    public void A_formula_survives_a_round_trip_and_a_column_remap()
    {
        var formula = new LedgerFormula([A, B], [new LedgerFormulaTerm(Total, -1)]);
        var replacement = Guid.NewGuid();

        var reread = LedgerFormula.Parse(formula.ToJson());
        var remapped = reread!.Remap(new Dictionary<Guid, Guid> { [A] = replacement });

        Assert.Equal(new[] { replacement, B }, remapped.Product);
        Assert.Equal(-1, remapped.Terms.Single().Sign);
        Assert.Null(LedgerFormula.Parse("not json"));
        Assert.Null(LedgerFormula.Parse(null));
    }
}
