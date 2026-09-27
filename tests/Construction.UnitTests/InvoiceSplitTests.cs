using Construction.Application.Features.Invoices;

namespace Construction.UnitTests;

/// <summary>Splitting an invoice among companies: the parts always add up to the whole, to the cent.</summary>
public class InvoiceSplitTests
{
    [Theory]
    [InlineData(100, 3)]
    [InlineData(100, 1)]
    [InlineData(100.01, 2)]
    [InlineData(0.05, 4)]
    [InlineData(999999.99, 7)]
    [InlineData(-100.01, 3)]
    [InlineData(-0.07, 5)]
    public void The_parts_add_up_exactly(double amount, int parts)
    {
        var split = InvoiceRules.SplitEvenly((decimal)amount, parts);

        Assert.Equal(parts, split.Count);
        Assert.Equal((decimal)amount, split.Sum());
    }

    [Fact]
    public void What_a_cent_cannot_divide_goes_to_the_first_parts_one_cent_each()
    {
        Assert.Equal([33.34m, 33.33m, 33.33m], InvoiceRules.SplitEvenly(100m, 3));
        Assert.Equal([50.01m, 50.00m], InvoiceRules.SplitEvenly(100.01m, 2));
    }

    [Fact]
    public void No_part_differs_from_another_by_more_than_a_cent()
    {
        var split = InvoiceRules.SplitEvenly(123456.79m, 9);

        Assert.True(split.Max() - split.Min() <= 0.01m);
    }

    [Fact]
    public void A_credit_note_splits_the_same_way()
    {
        Assert.Equal([-33.34m, -33.33m, -33.33m], InvoiceRules.SplitEvenly(-100m, 3));
    }

    [Fact]
    public void Nobody_can_split_among_nobody()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvoiceRules.SplitEvenly(100m, 0));
    }
}
