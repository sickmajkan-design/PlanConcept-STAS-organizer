using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.FuelCards.Import;
using FluentValidation.Results;

namespace Construction.Application.Features.FuelTransactions;

/// <summary>One readable line of a DKV statement.</summary>
public record DkvRow(
    int RowNumber,
    string CardNumber,
    string? VehicleLabel,
    DateOnly Date,
    TimeOnly Time,
    string? ProductGroup,
    string? ProductType,
    string ProductCode,
    decimal Amount,
    string Currency,
    string? Country,
    bool IsInvoiced);

public record DkvParseError(int RowNumber, string Reason);

public record DkvParseResult(IReadOnlyList<DkvRow> Rows, IReadOnlyList<DkvParseError> Errors);

/// <summary>
/// Reads DKV's own export (the Slovenian-language "Fuel_Report" CSV) by column
/// heading rather than by position, so a reordered or extended export still
/// reads. Unlike the older mapping-based import, the layout is known: there are
/// no litres on it, only an amount.
/// </summary>
/// <remarks>
/// Headings are compared with every diacritic and non-letter stripped, and by
/// the short stem only, so a file saved in the wrong code page (č turning into a
/// replacement character) still finds its columns.
/// </remarks>
public static partial class DkvStatementParser
{
    private static readonly string[] TimestampFormats =
    [
        "dd.MM.yyyy - HH:mm", "dd.MM.yyyy - HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss",
        "d.M.yyyy - HH:mm", "d.M.yyyy HH:mm"
    ];

    public static DkvParseResult Parse(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows.Count == 0)
        {
            throw Invalid("The file has no rows.");
        }

        var headings = rows[0].Select(Normalise).ToList();

        var card = Find(headings, "kartic");
        var label = Find(headings, "registrsk");
        var time = Find(headings, "transakcij");
        var status = Find(headings, "status");
        var code = Find(headings, "kodaizdelka");
        var amount = Find(headings, "vrednost");

        var missing = new List<string>();
        if (card < 0) missing.Add("card number (Št. kartice)");
        if (label < 0) missing.Add("registration (Registrska številka vozila)");
        if (time < 0) missing.Add("transaction time (Čas transakcije)");
        if (status < 0) missing.Add("invoice status (Status računa)");
        if (code < 0) missing.Add("product code (Koda izdelka)");
        if (amount < 0) missing.Add("gross value (Skupna vrednost bruto)");

        if (missing.Count > 0)
        {
            throw Invalid("This does not look like a DKV statement. Missing columns: "
                + string.Join(", ", missing) + ".");
        }

        var group = Find(headings, "skupinaizdelkov");
        var type = Find(headings, "vrstaizdelka");
        var country = Find(headings, "storitve");

        var parsed = new List<DkvRow>();
        var errors = new List<DkvParseError>();

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 1;

            var cardNumber = Cell(row, card)?.Trim();
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                errors.Add(new DkvParseError(rowNumber, "No card number."));
                continue;
            }

            if (!DateTime.TryParseExact(
                    Cell(row, time)?.Trim(), TimestampFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var moment))
            {
                errors.Add(new DkvParseError(rowNumber, "Could not read the transaction time."));
                continue;
            }

            if (!TryParseMoney(Cell(row, amount), out var value, out var currency))
            {
                errors.Add(new DkvParseError(rowNumber, "Could not read the amount."));
                continue;
            }

            parsed.Add(new DkvRow(
                rowNumber,
                cardNumber,
                NullIfBlank(Cell(row, label)),
                DateOnly.FromDateTime(moment),
                TimeOnly.FromDateTime(moment),
                NullIfBlank(Cell(row, group)),
                NullIfBlank(Cell(row, type)),
                (Cell(row, code) ?? "").Trim(),
                value,
                currency,
                NullIfBlank(Cell(row, country)),
                IsInvoicedValue(Cell(row, status))));
        }

        return new DkvParseResult(parsed, errors);
    }

    /// <summary>"Obračunano" is invoiced; "Ni obračunano" is not yet.</summary>
    internal static bool IsInvoicedValue(string? raw) => Normalise(raw ?? "") == "obracunano";

    internal static bool TryParseMoney(string? raw, out decimal value, out string currency)
    {
        value = 0;
        currency = "EUR";

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var match = MoneyPattern().Match(raw.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (match.Groups["cur"].Success)
        {
            currency = match.Groups["cur"].Value.ToUpperInvariant();
        }

        return FuelStatementValueParser.TryParseDecimal(match.Groups["num"].Value, out value);
    }

    /// <summary>Lower-case letters only, diacritics removed.</summary>
    internal static string Normalise(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetter(c) && c < 128)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static int Find(IReadOnlyList<string> headings, string stem)
    {
        for (var i = 0; i < headings.Count; i++)
        {
            if (headings[i].Contains(stem, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? Cell(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] : null;

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ValidationException Invalid(string message) =>
        new([new ValidationFailure("file", message)]);

    [GeneratedRegex(@"^(?<num>-?[\d.,\s]+?)\s*(?<cur>[A-Za-z]{3})?$")]
    private static partial Regex MoneyPattern();
}
