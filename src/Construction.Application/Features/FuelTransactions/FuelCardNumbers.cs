namespace Construction.Application.Features.FuelTransactions;

/// <summary>
/// A card number is typed by a driver reading it off plastic, so spaces and case cannot be trusted.
/// </summary>
public static class FuelCardNumbers
{
    /// <summary>What is stored: trimmed, or null when nothing was entered.</summary>
    public static string? Clean(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>What two numbers are compared by: no spaces or dashes, any case.</summary>
    public static string Normalise(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
