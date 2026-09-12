using System.Text.Json;
using Anthropic.Models.Messages;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SdkClient = Anthropic.AnthropicClient;
using SdkRole = Anthropic.Models.Messages.Role;

namespace Construction.Infrastructure.Ai;

/// <summary>
/// The assistant's transport, and the only place the Anthropic SDK appears.
/// </summary>
public sealed class ClaudeAssistantClient : IAssistantClient
{
    private readonly AnthropicSettings _settings;
    private readonly ILogger<ClaudeAssistantClient> _logger;
    private readonly SdkClient? _client;

    public ClaudeAssistantClient(
        IOptions<AnthropicSettings> settings,
        ILogger<ClaudeAssistantClient> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Constructed once. Without a key it stays null and IsConfigured says
        // so, rather than being built and failing on the first question.
        _client = _settings.IsConfigured
            ? new SdkClient { ApiKey = _settings.ApiKey }
            : null;
    }

    public bool IsConfigured => _client is not null;

    public IAssistantSession Start(AssistantSessionOptions options)
    {
        if (_client is null)
        {
            throw new ServiceUnavailableException(
                "The assistant is not configured on this installation.");
        }

        return new ClaudeAssistantSession(_client, _settings, _logger, options);
    }
}

/// <summary>
/// One exchange. Holds the running message list because continuing a tool-use
/// conversation means echoing the model's own blocks back with their ids
/// intact — see <see cref="IAssistantSession"/>.
/// </summary>
internal sealed class ClaudeAssistantSession : IAssistantSession
{
    private readonly SdkClient _client;
    private readonly AnthropicSettings _settings;
    private readonly ILogger _logger;
    private readonly AssistantSessionOptions _options;
    private readonly List<MessageParam> _messages = [];

    public ClaudeAssistantSession(
        SdkClient client,
        AnthropicSettings settings,
        ILogger logger,
        AssistantSessionOptions options)
    {
        _client = client;
        _settings = settings;
        _logger = logger;
        _options = options;

        foreach (var message in options.History)
        {
            _messages.Add(new MessageParam
            {
                Role = message.Role == AssistantRole.User ? SdkRole.User : SdkRole.Assistant,
                Content = message.Text,
            });
        }
    }

    public Task<AssistantTurn> SendAsync(string userText, CancellationToken cancellationToken = default)
    {
        _messages.Add(new MessageParam { Role = SdkRole.User, Content = userText });

        return CompleteAsync(cancellationToken);
    }

    public Task<AssistantTurn> ProvideToolResultsAsync(
        IReadOnlyList<AssistantToolOutcome> outcomes,
        CancellationToken cancellationToken = default)
    {
        // Every tool_use id must come back with exactly one tool_result or the
        // API refuses the whole follow-up, so the caller's outcomes are echoed
        // one for one and in order.
        List<ContentBlockParam> results = [];

        foreach (var outcome in outcomes)
        {
            results.Add(new ToolResultBlockParam
            {
                ToolUseID = outcome.Id,
                Content = outcome.Content,
                IsError = outcome.IsError,
            });
        }

        _messages.Add(new MessageParam { Role = SdkRole.User, Content = results });

        return CompleteAsync(cancellationToken);
    }

    private async Task<AssistantTurn> CompleteAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        Message response;

        try
        {
            response = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _settings.Model,
                    MaxTokens = _settings.MaxOutputTokens,

                    // The prompt and the tool list are identical on every turn
                    // of every conversation, so marking the prefix cacheable
                    // is the difference between paying for them once per
                    // question and once per round trip.
                    System = new List<TextBlockParam>
                    {
                        new()
                        {
                            Text = _options.SystemPrompt,
                            CacheControl = new CacheControlEphemeral(),
                        },
                    },
                    Tools = BuildTools(_options.Tools),
                    Messages = _messages,
                },
                cancellationToken: timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServiceUnavailableException(
                "The assistant took too long to answer. Please try again.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The message is deliberately generic: an SDK exception can carry
            // request details, and this text reaches the browser.
            _logger.LogWarning(exception, "The assistant API call failed");

            throw new ServiceUnavailableException(
                "The assistant could not be reached. Please try again.",
                exception);
        }

        return Consume(response);
    }

    /// <summary>
    /// Records what the model said, and reports what it asked for.
    /// </summary>
    private AssistantTurn Consume(Message response)
    {
        List<ContentBlockParam> echo = [];
        List<AssistantToolCall> calls = [];
        var text = new System.Text.StringBuilder();

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock))
            {
                text.Append(textBlock.Text);
                echo.Add(new TextBlockParam { Text = textBlock.Text });
            }
            else if (block.TryPickThinking(out ThinkingBlock? thinking))
            {
                // Carried back untouched. The signature is checked by the API,
                // so a re-serialised or trimmed copy is rejected.
                echo.Add(new ThinkingBlockParam
                {
                    Thinking = thinking.Thinking,
                    Signature = thinking.Signature,
                });
            }
            else if (block.TryPickRedactedThinking(out RedactedThinkingBlock? redacted))
            {
                echo.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
            }
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
            {
                echo.Add(new ToolUseBlockParam
                {
                    ID = toolUse.ID,
                    Name = toolUse.Name,
                    Input = toolUse.Input,
                });

                calls.Add(new AssistantToolCall(
                    toolUse.ID,
                    toolUse.Name,
                    JsonSerializer.Serialize(toolUse.Input)));
            }
        }

        _messages.Add(new MessageParam { Role = SdkRole.Assistant, Content = echo });

        return new AssistantTurn(
            text.ToString().Trim(),
            calls,
            new AssistantUsage(
                (int)response.Usage.InputTokens,
                (int)response.Usage.OutputTokens));
    }

    /// <summary>
    /// Turns the toolset's JSON schemas into what the SDK expects.
    /// </summary>
    private static List<ToolUnion> BuildTools(IReadOnlyList<AssistantToolDescriptor> tools)
    {
        List<ToolUnion> built = [];

        foreach (var tool in tools)
        {
            using var document = JsonDocument.Parse(tool.InputSchemaJson);
            var root = document.RootElement;

            var properties = new Dictionary<string, JsonElement>();

            if (root.TryGetProperty("properties", out var declared))
            {
                foreach (var property in declared.EnumerateObject())
                {
                    properties[property.Name] = property.Value.Clone();
                }
            }

            List<string> required = [];

            if (root.TryGetProperty("required", out var requiredNames))
            {
                foreach (var name in requiredNames.EnumerateArray())
                {
                    if (name.GetString() is { } value)
                    {
                        required.Add(value);
                    }
                }
            }

            built.Add(new Tool
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = new()
                {
                    Properties = properties,
                    Required = required,
                },
            });
        }

        return built;
    }
}
