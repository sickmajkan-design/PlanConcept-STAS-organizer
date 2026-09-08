using Construction.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Assistant;

public sealed record AskAssistantCommand : IRequest<AssistantAnswer>
{
    public string Message { get; init; } = string.Empty;

    /// <summary>Earlier turns, held by the panel and replayed as text.</summary>
    public IReadOnlyList<AssistantMessage> History { get; init; } = [];

    /// <summary>Which language to answer in — the panel's current locale.</summary>
    public string Locale { get; init; } = AssistantPrompt.SerbianLocale;
}

/// <param name="Text">The answer, in the caller's language.</param>
/// <param name="ToolsUsed">Named so the panel can show what was consulted.</param>
/// <param name="Truncated">True when the tool budget ran out before an answer.</param>
public sealed record AssistantAnswer(
    string Text,
    IReadOnlyList<string> ToolsUsed,
    bool Truncated);

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    /// <summary>Long enough for a real question, short enough not to be a paste of a table.</summary>
    public const int MaxMessageLength = 4000;

    /// <summary>
    /// The panel is not trusted to bound this. History is billed on every turn
    /// and arrives from the browser, so a client that kept appending would
    /// otherwise raise the cost of each request without limit.
    /// </summary>
    public const int MaxHistoryMessages = 20;

    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(MaxMessageLength);

        RuleFor(x => x.History)
            .NotNull()
            .Must(history => history.Count <= MaxHistoryMessages)
            .WithMessage($"A conversation may carry at most {MaxHistoryMessages} earlier messages.");

        RuleForEach(x => x.History)
            .Must(message => !string.IsNullOrWhiteSpace(message.Text))
            .WithMessage("An earlier message cannot be empty.")
            .Must(message => message.Text.Length <= MaxMessageLength)
            .WithMessage($"An earlier message cannot exceed {MaxMessageLength} characters.");

        RuleFor(x => x.Locale)
            .Must(locale =>
                locale == AssistantPrompt.SerbianLocale || locale == AssistantPrompt.EnglishLocale)
            .WithMessage("Locale must be 'sr' or 'en'.");
    }
}

public sealed class AskAssistantCommandHandler
    : IRequestHandler<AskAssistantCommand, AssistantAnswer>
{
    /// <summary>
    /// How many times the model may ask for tools before the answer is due.
    /// </summary>
    /// <remarks>
    /// A question worth asking here needs two or three lookups — who is on the
    /// site, then their hours. Six is generous for that and still bounds both
    /// the bill and the time the caller waits, which an unbounded loop does
    /// not.
    /// </remarks>
    public const int MaxToolIterations = 6;

    private readonly IAssistantClient _client;
    private readonly IAssistantToolset _toolset;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AskAssistantCommandHandler> _logger;

    public AskAssistantCommandHandler(
        IAssistantClient client,
        IAssistantToolset toolset,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<AskAssistantCommandHandler> logger)
    {
        _client = client;
        _toolset = toolset;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<AssistantAnswer> Handle(
        AskAssistantCommand request,
        CancellationToken cancellationToken)
    {
        var tools = await _toolset.DescribeAsync(cancellationToken);

        var session = _client.Start(new AssistantSessionOptions(
            AssistantPrompt.Build(
                _currentUserService.Email,
                _currentUserService.Role,
                _dateTimeProvider.UtcNow,
                request.Locale),
            tools,
            request.History));

        var turn = await session.SendAsync(request.Message, cancellationToken);
        var usage = turn.Usage;
        var used = new List<string>();

        var iterations = 0;

        while (turn.ToolCalls.Count > 0 && iterations < MaxToolIterations)
        {
            iterations++;

            var outcomes = new List<AssistantToolOutcome>(turn.ToolCalls.Count);

            foreach (var call in turn.ToolCalls)
            {
                // Every call is named in the log with the caller and the
                // correlation id already on the scope. Nothing else records
                // what the assistant looked at, since the conversation itself
                // is not stored.
                _logger.LogInformation(
                    "Assistant tool {ToolName} invoked by {UserId}",
                    call.Name,
                    _currentUserService.UserId);

                used.Add(call.Name);
                outcomes.Add(await _toolset.InvokeAsync(call, cancellationToken));
            }

            turn = await session.ProvideToolResultsAsync(outcomes, cancellationToken);
            usage += turn.Usage;
        }

        var truncated = turn.ToolCalls.Count > 0;

        if (truncated)
        {
            _logger.LogWarning(
                "Assistant stopped at the {Limit}-step tool budget for {UserId}",
                MaxToolIterations,
                _currentUserService.UserId);
        }

        _logger.LogInformation(
            "Assistant answered {UserId} using {ToolCount} tool calls, {InputTokens} in / {OutputTokens} out",
            _currentUserService.UserId,
            used.Count,
            usage.InputTokens,
            usage.OutputTokens);

        return new AssistantAnswer(
            turn.Text,
            used.Distinct(StringComparer.Ordinal).ToList(),
            truncated);
    }
}
