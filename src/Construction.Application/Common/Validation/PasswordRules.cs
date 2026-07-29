using FluentValidation;

namespace Construction.Application.Common.Validation;

public static class PasswordRules
{
    /// <summary>
    /// Company-wide password policy: at least 8 characters with upper case,
    /// lower case and a digit. Applied to every place a password is set.
    /// </summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one upper-case letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lower-case letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
