using Construction.Application.Common.Validation;
using FluentValidation;

namespace Construction.UnitTests;

/// <summary>The password policy: the length and mix of characters, and not one of the commonly chosen words.</summary>
public class PasswordRulesTests
{
    private sealed class Holder
    {
        public string Password { get; init; } = string.Empty;
    }

    private sealed class HolderValidator : AbstractValidator<Holder>
    {
        public HolderValidator() => RuleFor(x => x.Password).StrongPassword();
    }

    private static bool Accepts(string password) =>
        new HolderValidator().Validate(new Holder { Password = password }).IsValid;

    [Theory]
    [InlineData("Password1")]
    [InlineData("Password123!")]
    [InlineData("Passw0rd!")]
    [InlineData("P@ssword1")]
    [InlineData("QWERTY123")]
    [InlineData("Admin123")]
    [InlineData("Lozinka1")]
    [InlineData("Welcome2024")]
    public void A_commonly_chosen_password_is_refused_however_it_is_dressed_up(string password)
    {
        Assert.False(Accepts(password));
    }

    [Theory]
    [InlineData("Test1234!")]
    [InlineData("SamePassword1!")]
    [InlineData("Konj-Zelena-Cigla-7")]
    [InlineData("PasswordManagerKey9")]
    public void An_ordinary_password_that_merely_contains_a_common_word_is_accepted(string password)
    {
        Assert.True(Accepts(password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ab1")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void The_length_and_character_rules_still_apply(string password)
    {
        Assert.False(Accepts(password));
    }
}
