using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Assistant;

namespace Construction.UnitTests.Validation;

/// <summary>
/// What the panel is allowed to send.
/// </summary>
/// <remarks>
/// The bounds here are cost controls rather than correctness ones, which makes
/// them easy to leave out. History is billed on every turn and arrives from a
/// browser, so a client that simply kept appending — a bug, not an attack —
/// would raise the price of each question without limit and without anyone
/// noticing until the invoice.
/// </remarks>
public class AskAssistantValidatorTests
{
    private readonly AskAssistantCommandValidator _validator = new();

    private static AskAssistantCommand Valid() => new()
    {
        Message = "Koliko sati je Marko radio u julu?",
        Locale = "sr",
    };

    [Fact]
    public void Accepts_an_ordinary_question()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Rejects_an_empty_question()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with { Message = "   " },
            nameof(AskAssistantCommand.Message));
    }

    [Fact]
    public void Rejects_a_question_longer_than_the_limit()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                Message = new string('x', AskAssistantCommandValidator.MaxMessageLength + 1),
            },
            nameof(AskAssistantCommand.Message));
    }

    [Fact]
    public void Accepts_a_conversation_at_the_history_limit()
    {
        ValidationAssert.Valid(
            _validator,
            Valid() with { History = Turns(AskAssistantCommandValidator.MaxHistoryMessages) });
    }

    [Fact]
    public void Rejects_a_conversation_past_the_history_limit()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with { History = Turns(AskAssistantCommandValidator.MaxHistoryMessages + 1) },
            nameof(AskAssistantCommand.History));
    }

    [Fact]
    public void Rejects_an_oversized_earlier_message()
    {
        // Otherwise the per-message limit is trivially bypassed by putting the
        // payload in the history instead of the question.
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                History =
                [
                    new AssistantMessage(
                        AssistantRole.User,
                        new string('x', AskAssistantCommandValidator.MaxMessageLength + 1)),
                ],
            },
            "History[0]");
    }

    [Theory]
    [InlineData("sr")]
    [InlineData("en")]
    public void Accepts_both_languages_the_product_ships(string locale)
    {
        ValidationAssert.Valid(_validator, Valid() with { Locale = locale });
    }

    [Fact]
    public void Rejects_a_locale_the_product_does_not_have()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with { Locale = "de" },
            nameof(AskAssistantCommand.Locale));
    }

    private static IReadOnlyList<AssistantMessage> Turns(int count) =>
        Enumerable
            .Range(0, count)
            .Select(i => new AssistantMessage(
                i % 2 == 0 ? AssistantRole.User : AssistantRole.Assistant,
                $"turn {i}"))
            .ToList();
}
