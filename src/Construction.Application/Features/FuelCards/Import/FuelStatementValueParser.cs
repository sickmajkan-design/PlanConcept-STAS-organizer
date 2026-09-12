using System.Globalization;

namespace Construction.Application.Features.FuelCards.Import;

/// <summary>
/// Parses the individual cell values off a statement defensively, since there
/// is no real DKV sample to calibrate the exact format against yet.
/// </summary>
/// <remarks>
/// Formats supported, deliberately wider than one locale:
/// dates as <c>dd.MM.yyyy</c> (with or without a trailing dot, the common
/// European/Balkan style) or <c>yyyy-MM-dd</c> (ISO); amounts and litres with
/// either <c>,</c> or <c>.</c> as the decimal separator, and an optional
/// thousands separator on the other of the two.
/// </remarks>
public static class FuelStatementValueParser
{
    private static readonly string[] DateFormats =
    [
        "dd.MM.yyyy.", "dd.MM.yyyy", "d.M.yyyy.", "d.M.yyyy",
        "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"
    ];

    public static bool TryParseDate(string? raw, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();

        if (DateOnly.TryParseExact(
            trimmed, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static bool TryParseDecimal(string? raw, out decimal value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // Strips a plain space and a non-breaking space ( ), both of
        // which show up as thousands-grouping in exported currency columns.
        var trimmed = raw.Trim()
            .Replace(" ", "")
            .Replace(" ", "");

        var hasComma = trimmed.Contains(',');
        var hasDot = trimmed.Contains('.');

        string normalised;

        if (hasComma && hasDot)
        {
            // Whichever separator appears last is the decimal point; the
            // other one is a thousands grouping and gets dropped.
            var lastComma = trimmed.LastIndexOf(',');
            var lastDot = trimmed.LastIndexOf('.');

            normalised = lastComma > lastDot
                ? trimmed.Replace(".", "").Replace(',', '.')
                : trimmed.Replace(",", "");
        }
        else if (hasComma)
        {
            normalised = trimmed.Replace(',', '.');
        }
        else
        {
            normalised = trimmed;
        }

        return decimal.TryParse(
            normalised, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
