using FluentValidation;

namespace Construction.Application.Common.Validation;

public static class PasswordRules
{
    /// <summary>
    /// Words that, with a capital and a number added, are what people actually choose. Compared after the digits,
    /// symbols and look-alike letters are stripped (<c>Passw0rd!</c> and <c>Password123</c> are both "password"), and
    /// only for an exact match, so a long passphrase that merely contains one is fine.
    /// </summary>
    /// <remarks>
    /// shortcut: a short local list, not a breach database. Upgrade to a full list (the ten thousand most common
    /// passwords, bundled as a resource) or a k-anonymity breach check when sending a hash prefix out is acceptable.
    /// </remarks>
    private static readonly HashSet<string> CommonBases = new(StringComparer.Ordinal)
    {
        "password", "passwort", "qwerty", "qwertz", "qwertyuiop", "welcome", "admin", "administrator", "letmein",
        "iloveyou", "monkey", "dragon", "master", "login", "secret", "changeme", "abc", "abcdef", "asdf", "asdfgh",
        "zxcvbn", "football", "baseball", "sunshine", "princess", "trustno", "lozinka", "sifra", "zaporka", "srbija",
        "hrvatska", "bosna", "beograd", "sarajevo", "zagreb", "gradiliste", "radnik", "firma", "zima", "ljeto",
        "proljece", "jesen", "construction", "organizer"
    };

    /// <summary>True when the password is one of the commonly chosen ones, ignoring digits, symbols and case.</summary>
    internal static bool IsCommon(string password)
    {
        // The digits and symbols people add at the end ("1", "123!", "2024") come off first, so that a trailing digit is
        // not read as a look-alike letter ("Password1" must not become "passwordi").
        var lower = password.ToLowerInvariant();
        var end = lower.Length;

        while (end > 0 && !char.IsLetter(lower[end - 1]))
        {
            end--;
        }

        var stem = lower[..end];
        var letters = new System.Text.StringBuilder(stem.Length);

        foreach (var c in stem)
        {
            letters.Append(c switch { '0' => 'o', '@' => 'a', '$' => 's', '3' => 'e', '4' => 'a', '1' => 'i', _ => c });
        }

        var bare = new string(letters.ToString().Where(char.IsLetter).ToArray());

        return CommonBases.Contains(bare);
    }

    /// <summary>
    /// Company-wide password policy: at least 8 characters with upper case,
    /// lower case and a digit, and not one of the commonly chosen passwords.
    /// Applied to every place a password is set.
    /// </summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one upper-case letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lower-case letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Must(password => string.IsNullOrEmpty(password) || !IsCommon(password)).WithMessage("That password is too common. Choose another.");
    }
}
