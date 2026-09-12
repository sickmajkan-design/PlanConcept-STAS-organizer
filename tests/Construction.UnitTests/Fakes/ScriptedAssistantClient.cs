using Construction.Application.Common.Interfaces;

namespace Construction.UnitTests.Fakes;

/// <summary>
/// An assistant that says what the test told it to say.
/// </summary>
/// <remarks>
/// The same shape as <c>RecordingEmailSender</c> and <c>RecordingPushSender</c>
/// in the integration fixture: the loop, the tool budget and the answer are
/// all testable without a network call, an API key or a bill.
/// </remarks>
public sealed class ScriptedAssistantClient : IAssistantClient
{
    private readonly Queue<AssistantTurn> _turns;

    public ScriptedAssistantClient(params AssistantTurn[] turns)
    {
        _turns = new Queue<AssistantTurn>(turns);
    }

    public bool IsConfigured { get; set; } = true;

    /// <summary>The options the handler started with — prompt, tools, history.</summary>
    public AssistantSessionOptions? StartedWith { get; private set; }

    /// <summary>What was asked, then each batch of tool results, in order.</summary>
    public List<string> Sent { get; } = [];

    public List<IReadOnlyList<AssistantToolOutcome>> Provided { get; } = [];

    public IAssistantSession Start(AssistantSessionOptions options)
    {
        StartedWith = options;

        return new Session(this);
    }

    private AssistantTurn Next() =>
        _turns.Count > 0
            ? _turns.Dequeue()
            : new AssistantTurn(string.Empty, [], AssistantUsage.None);

    private sealed class Session : IAssistantSession
    {
        private readonly ScriptedAssistantClient _owner;

        public Session(ScriptedAssistantClient owner) => _owner = owner;

        public Task<AssistantTurn> SendAsync(string userText, CancellationToken cancellationToken = default)
        {
            _owner.Sent.Add(userText);

            return Task.FromResult(_owner.Next());
        }

        public Task<AssistantTurn> ProvideToolResultsAsync(
            IReadOnlyList<AssistantToolOutcome> outcomes,
            CancellationToken cancellationToken = default)
        {
            _owner.Provided.Add(outcomes);

            return Task.FromResult(_owner.Next());
        }
    }
}

/// <summary>A toolset that answers from a dictionary, and records what was asked.</summary>
public sealed class ScriptedToolset : IAssistantToolset
{
    private readonly Dictionary<string, string> _answers;

    public ScriptedToolset(params (string Name, string Result)[] answers)
    {
        _answers = answers.ToDictionary(a => a.Name, a => a.Result, StringComparer.Ordinal);
    }

    public List<AssistantToolDescriptor> Offered { get; } =
    [
        new("list_projects", "Sites.", """{ "type": "object", "properties": {} }"""),
    ];

    public List<AssistantToolCall> Invoked { get; } = [];

    public Task<IReadOnlyList<AssistantToolDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AssistantToolDescriptor>>(Offered);

    public Task<AssistantToolOutcome> InvokeAsync(
        AssistantToolCall call,
        CancellationToken cancellationToken = default)
    {
        Invoked.Add(call);

        return Task.FromResult(_answers.TryGetValue(call.Name, out var result)
            ? new AssistantToolOutcome(call.Id, result, false)
            : new AssistantToolOutcome(call.Id, $"There is no tool called '{call.Name}'.", true));
    }
}
