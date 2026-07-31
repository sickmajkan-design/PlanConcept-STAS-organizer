using FluentValidation;

namespace Construction.UnitTests;

/// <summary>
/// Assertions for FluentValidation results. Errors are matched on property
/// name so a test states which field it expects to be rejected, rather than
/// only that validation failed for some reason.
/// </summary>
public static class ValidationAssert
{
    public static void Valid<T>(IValidator<T> validator, T instance)
    {
        var result = validator.Validate(instance);

        Assert.True(
            result.IsValid,
            "Expected the request to be valid, but got: " +
            string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
    }

    public static void Invalid<T>(IValidator<T> validator, T instance, string expectedProperty)
    {
        var result = validator.Validate(instance);

        Assert.False(result.IsValid, $"Expected '{expectedProperty}' to be rejected, but the request validated.");

        Assert.True(
            result.Errors.Any(e => string.Equals(
                e.PropertyName, expectedProperty, StringComparison.OrdinalIgnoreCase)),
            $"Expected an error on '{expectedProperty}', but got: " +
            string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
    }
}
