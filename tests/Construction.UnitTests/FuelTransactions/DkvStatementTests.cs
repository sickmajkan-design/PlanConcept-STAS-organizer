using Construction.Application.Features.FuelTransactions;
using Construction.Domain.Enums;
using Construction.Application.Common.Exceptions;
using Xunit;

namespace Construction.UnitTests.FuelTransactions;

public class DkvStatementParserTests
{
    private const string Header =
        "Št. kartice/kataloga;Registrska številka vozila;Čas transakcije;Status računa;Stroškovna skupina;"
        + "Skupina izdelkov;Vrsta izdelka;Koda izdelka;Skupna vrednost bruto (valuta računa);Država storitve";

    private static IReadOnlyList<IReadOnlyList<string>> Rows(params string[] lines) =>
        new[] { Header }.Concat(lines).Select(l => (IReadOnlyList<string>)l.Split(';')).ToList();

    [Fact]
    public void Reads_a_real_statement_row()
    {
        var result = DkvStatementParser.Parse(Rows(
            "70431001138023874;15;01.09.2026 - 06:37;Obračunano;Stroški goriva;Dizelsko gorivo;Dizelsko gorivo;WA0009;140.02 EUR;DE"));

        var row = Assert.Single(result.Rows);
        Assert.Equal("70431001138023874", row.CardNumber);
        Assert.Equal("15", row.VehicleLabel);
        Assert.Equal(new DateOnly(2026, 9, 1), row.Date);
        Assert.Equal(new TimeOnly(6, 37), row.Time);
        Assert.Equal(140.02m, row.Amount);
        Assert.Equal("EUR", row.Currency);
        Assert.Equal("WA0009", row.ProductCode);
        Assert.Equal("DE", row.Country);
        Assert.True(row.IsInvoiced);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Not_yet_invoiced_rows_are_marked_so()
    {
        var result = DkvStatementParser.Parse(Rows(
            "70431001134499524;SD SMART;16.09.2026 - 18:06;Ni obračunano;Stroški goriva;Dizelsko gorivo;Dizelsko gorivo;WA0009;143.53 EUR;DE"));

        Assert.False(Assert.Single(result.Rows).IsInvoiced);
    }

    [Fact]
    public void Finds_columns_even_when_the_accents_were_mangled()
    {
        var mangled = Header.Replace("Š", "?").Replace("č", "?").Replace("Č", "?").Replace("š", "?").Replace("ž", "?");
        var rows = new List<IReadOnlyList<string>>
        {
            mangled.Split(';'),
            "70431001138023874;15;01.09.2026 - 06:37;Obra?unano;x;Dizelsko gorivo;Dizelsko gorivo;WA0009;140.02 EUR;DE".Split(';')
        };

        var row = Assert.Single(DkvStatementParser.Parse(rows).Rows);
        Assert.Equal(140.02m, row.Amount);
    }

    [Fact]
    public void A_row_that_cannot_be_read_is_reported_not_dropped_silently()
    {
        var result = DkvStatementParser.Parse(Rows(
            "70431001138023874;15;not-a-date;Obračunano;x;y;z;WA0009;140.02 EUR;DE",
            "70431001138023874;15;01.09.2026 - 06:37;Obračunano;x;y;z;WA0009;lots;DE"));

        Assert.Empty(result.Rows);
        Assert.Equal([2, 3], result.Errors.Select(e => e.RowNumber));
    }

    [Fact]
    public void A_file_that_is_not_a_dkv_statement_is_refused_with_the_missing_columns_named()
    {
        var rows = new List<IReadOnlyList<string>> { new[] { "Name", "Amount" }, new[] { "a", "1" } };

        var error = Assert.Throws<ValidationException>(() => DkvStatementParser.Parse(rows));

        var message = Assert.Single(error.Errors["file"]);
        Assert.Contains("card number", message);
        Assert.Contains("transaction time", message);
    }
}

public class DkvMatcherTests
{
    private static readonly Guid VehicleA = Guid.NewGuid();
    private static readonly Guid VehicleB = Guid.NewGuid();

    private static readonly DkvVehicleInfo A = new(VehicleA, "Van A", "15", "ZG-1", FuelType.Diesel);
    private static readonly DkvVehicleInfo B = new(VehicleB, "Van B", "16", "ZG-2", FuelType.Diesel);

    private static readonly IReadOnlySet<string> Labels = new HashSet<string> { "15", "16", "zg-1", "zg-2" };

    private static readonly Dictionary<string, DkvVehicleInfo> Cards = new()
    {
        ["card-a"] = A,
        ["card-b"] = B
    };

    private static DkvRow Row(
        string card = "card-a",
        string? label = "SD SMART",
        string date = "2026-09-01",
        decimal amount = 100m,
        string product = "Dizelsko gorivo",
        int minute = 0) =>
        new(1, card, label, DateOnly.Parse(date), new TimeOnly(12, minute), "Dizelsko gorivo", product,
            "WA0009", amount, "EUR", "DE", true);

    private static DkvExpenseInfo Expense(
        Guid vehicle, string date, decimal amount, bool odometer = true, bool receipt = true) =>
        new(Guid.NewGuid(), vehicle, DateOnly.Parse(date), amount, odometer, receipt);

    private static DkvMatch Single(DkvRow row, params DkvExpenseInfo[] expenses) =>
        Assert.Single(DkvMatcher.Match([row], Cards, expenses, Labels));

    [Fact]
    public void Same_vehicle_day_and_amount_is_a_match()
    {
        var expense = Expense(VehicleA, "2026-09-01", 100m);

        var match = Single(Row(), expense);

        Assert.Equal(FuelTransactionStatus.Matched, match.Status);
        Assert.Equal(expense.Id, match.ExpenseId);
    }

    [Theory]
    [InlineData(false, true, "odometer")]
    [InlineData(true, false, "receipt photo")]
    public void An_entry_that_agrees_but_lacks_the_odometer_or_the_receipt_needs_review(
        bool odometer, bool receipt, string expectedInDetail)
    {
        var match = Single(Row(), Expense(VehicleA, "2026-09-01", 100m, odometer, receipt));

        Assert.Equal(FuelTransactionStatus.NeedsReview, match.Status);
        Assert.Equal(FuelTransactionIssue.IncompleteEntry, match.Issue);
        Assert.Contains(expectedInDetail, match.Detail);
    }

    [Fact]
    public void An_entry_on_the_neighbouring_day_still_matches_when_the_amount_agrees()
    {
        // A fill-up at 00:15 is often entered under the evening before.
        var match = Single(Row(date: "2026-09-05"), Expense(VehicleA, "2026-09-04", 100m));

        Assert.Equal(FuelTransactionStatus.Matched, match.Status);
    }

    [Fact]
    public void Another_vehicles_entry_never_matches()
    {
        var match = Single(Row(), Expense(VehicleB, "2026-09-01", 100m));

        Assert.Equal(FuelTransactionStatus.NoDriverEntry, match.Status);
        Assert.Null(match.ExpenseId);
    }

    [Fact]
    public void A_different_amount_on_the_same_day_needs_review_and_shows_both_figures()
    {
        var expense = Expense(VehicleA, "2026-09-01", 80m);

        var match = Single(Row(amount: 100m), expense);

        Assert.Equal(FuelTransactionStatus.NeedsReview, match.Status);
        Assert.Equal(FuelTransactionIssue.AmountMismatch, match.Issue);
        Assert.Equal(expense.Id, match.ExpenseId);
        Assert.Contains("100.00", match.Detail);
        Assert.Contains("80.00", match.Detail);
    }

    [Fact]
    public void One_driver_entry_cannot_confirm_two_statement_rows()
    {
        var expense = Expense(VehicleA, "2026-09-01", 100m);

        var matches = DkvMatcher.Match(
            [Row(minute: 0), Row(minute: 5)], Cards, [expense], Labels);

        Assert.Equal(1, matches.Count(m => m.Status == FuelTransactionStatus.Matched));
        Assert.Equal(1, matches.Count(m => m.Status == FuelTransactionStatus.NoDriverEntry));
    }

    [Fact]
    public void Two_fills_on_one_day_each_pair_with_their_own_entry_by_amount()
    {
        var first = Expense(VehicleA, "2026-09-01", 44.92m);
        var second = Expense(VehicleA, "2026-09-01", 30.65m);

        var matches = DkvMatcher.Match(
            [Row(amount: 30.65m, minute: 0), Row(amount: 44.92m, minute: 5)], Cards, [first, second], Labels);

        Assert.All(matches, m => Assert.Equal(FuelTransactionStatus.Matched, m.Status));
        Assert.Equal(second.Id, matches[0].ExpenseId);
        Assert.Equal(first.Id, matches[1].ExpenseId);
    }

    [Fact]
    public void An_unknown_card_is_reported_as_such()
    {
        var match = Single(Row(card: "mystery"));

        Assert.Equal(FuelTransactionStatus.UnknownCard, match.Status);
        Assert.Null(match.VehicleId);
    }

    [Fact]
    public void The_fleet_wide_label_is_not_treated_as_a_vehicle_claim()
    {
        var match = Single(Row(label: "SD SMART"), Expense(VehicleA, "2026-09-01", 100m));

        Assert.Equal(FuelTransactionStatus.Matched, match.Status);
    }

    [Fact]
    public void A_td_that_belongs_to_another_vehicle_needs_review()
    {
        var match = Single(Row(card: "card-a", label: "16"), Expense(VehicleA, "2026-09-01", 100m));

        Assert.Equal(FuelTransactionStatus.NeedsReview, match.Status);
        Assert.Equal(FuelTransactionIssue.TdMismatch, match.Issue);
    }

    [Fact]
    public void The_cards_own_td_agrees()
    {
        var match = Single(Row(card: "card-a", label: "15"), Expense(VehicleA, "2026-09-01", 100m));

        Assert.Equal(FuelTransactionStatus.Matched, match.Status);
    }

    [Fact]
    public void A_numbered_label_on_a_vehicle_with_no_td_is_not_held_against_it()
    {
        var noTd = new DkvVehicleInfo(VehicleA, "Van A", null, "ZG-1", FuelType.Diesel);
        var cards = new Dictionary<string, DkvVehicleInfo> { ["card-a"] = noTd };

        var match = Assert.Single(DkvMatcher.Match([Row(label: "15")], cards, [], Labels));

        Assert.NotEqual(FuelTransactionIssue.TdMismatch, match.Issue);
    }

    [Fact]
    public void An_entry_naming_the_rows_card_matches_even_when_recorded_on_another_vehicle()
    {
        // The card of van A was used to fill van B.
        var expense = Expense(VehicleB, "2026-09-01", 100m) with { CardNumber = "Card A" };

        var match = Single(Row(card: "card-a"), expense);

        Assert.Equal(FuelTransactionStatus.Matched, match.Status);
        Assert.Equal(expense.Id, match.ExpenseId);
    }

    [Fact]
    public void An_entry_on_another_vehicle_with_another_card_is_not_taken()
    {
        var expense = Expense(VehicleB, "2026-09-01", 100m) with { CardNumber = "card-b" };

        var match = Single(Row(card: "card-a"), expense);

        Assert.Equal(FuelTransactionStatus.NoDriverEntry, match.Status);
    }

    [Fact]
    public void With_two_equal_entries_the_one_with_the_same_card_is_paired()
    {
        var other = Expense(VehicleA, "2026-09-01", 100m) with { CardNumber = "card-x" };
        var same = Expense(VehicleA, "2026-09-01", 100m) with { CardNumber = "CARD-A" };

        var match = Single(Row(card: "card-a"), other, same);

        Assert.Equal(same.Id, match.ExpenseId);
    }

    [Fact]
    public void A_mistyped_card_number_still_matches_on_the_vehicle()
    {
        var expense = Expense(VehicleA, "2026-09-01", 100m) with { CardNumber = "typo" };

        var match = Single(Row(card: "card-a"), expense);

        Assert.Equal(FuelTransactionStatus.Matched, match.Status);
    }

    [Fact]
    public void Petrol_on_a_diesel_vehicle_needs_review_but_adblue_does_not()
    {
        var petrol = Row() with { ProductGroup = "Bencin", ProductType = "Bencin - RON 95" };
        var adblue = Row(minute: 3) with { ProductGroup = "AdBlue", ProductType = "AdBlue (pločevinke)" };

        var matches = DkvMatcher.Match([petrol, adblue], Cards, [], Labels);

        Assert.Equal(FuelTransactionIssue.FuelTypeMismatch, matches[0].Issue);
        Assert.Equal(FuelTransactionStatus.NoDriverEntry, matches[1].Status);
    }
}
