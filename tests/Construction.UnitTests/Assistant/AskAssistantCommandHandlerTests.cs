using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Assistant;
using Construction.Domain.Enums;
using Construction.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Construction.UnitTests.Assistant;

/// <summary>
/// The loop between the model and the tools.
/// </summary>
/// <remarks>
/// What matters here is not that an answer comes back — the fake decides that
/// — but that the loop cannot run away. It is the only place in this system
/// where a third party decides how much work the server does, and each round
/// trip is billed.
/// </remarks>
public class AskAssistantCommandHandlerTests
{
    private static AssistantTurn Says(string text) =>
        new(text, [], new AssistantUsage(100, 20));

    private static AssistantTurn Calls(string tool, string id = "call-1") =>
        new(string.Empty, [new AssistantToolCall(id, tool, "{}")], new AssistantUsage(100, 20));

    private static AskAssistantCommandHandler Handler(
        IAssistantClient client,
        IAssistantToolset toolset,
        ICurrentUserService? user = null) =>
        new(client,
            toolset,
            user ?? new StubCurrentUser(),
            new FixedDateTimeProvider(),
            NullLogger<AskAssistantCommandHandler>.Instance);

    private static AskAssistantCommand Ask(string message = "Koliko sati je Marko radio u julu?") =>
        new() { Message = message };

    [Fact]
    public async Task An_answer_with_no_tool_call_comes_straight_back()
    {
        var client = new ScriptedAssistantClient(Says("Marko je radio 168 sati."));

        var answer = await Handler(client, new ScriptedToolset()).Handle(Ask(), default);

        Assert.Equal("Marko je radio 168 sati.", answer.Text);
        Assert.Empty(answer.ToolsUsed);
        Assert.False(answer.Truncated);
    }

    [Fact]
    public async Task A_tool_call_is_run_and_its_result_fed_back()
    {
        var client = new ScriptedAssistantClient(
            Calls("list_projects"),
            Says("Aktivna su tri gradilišta."));

        var toolset = new ScriptedToolset(("list_projects", """{"items":[]}"""));

        var answer = await Handler(client, toolset).Handle(Ask(), default);

        Assert.Equal("Aktivna su tri gradilišta.", answer.Text);
        Assert.Equal(new[] { "list_projects" }, answer.ToolsUsed);

        Assert.Single(toolset.Invoked);
        Assert.Equal("list_projects", toolset.Invoked[0].Name);

        // The result has to reach the model, or the answer is invented.
        Assert.Single(client.Provided);
        Assert.Equal("""{"items":[]}""", client.Provided[0][0].Content);
    }

    [Fact]
    public async Task The_loop_stops_at_the_tool_budget()
    {
        // A model that keeps asking for tools would otherwise run until the
        // request timed out, having spent a call each time round.
        var turns = Enumerable
            .Range(0, AskAssistantCommandHandler.MaxToolIterations + 5)
            .Select(i => Calls("list_projects", $"call-{i}"))
            .ToArray();

        var client = new ScriptedAssistantClient(turns);
        var toolset = new ScriptedToolset(("list_projects", "{}"));

        var answer = await Handler(client, toolset).Handle(Ask(), default);

        Assert.True(answer.Truncated);
        Assert.Equal(AskAssistantCommandHandler.MaxToolIterations, toolset.Invoked.Count);
    }

    [Fact]
    public async Task A_tool_named_more_than_once_is_reported_once()
    {
        var client = new ScriptedAssistantClient(
            Calls("list_projects", "a"),
            Calls("list_projects", "b"),
            Says("Gotovo."));

        var answer = await Handler(client, new ScriptedToolset(("list_projects", "{}")))
            .Handle(Ask(), default);

        Assert.Equal(new[] { "list_projects" }, answer.ToolsUsed);
    }

    [Fact]
    public async Task A_refused_tool_is_reported_to_the_model_rather_than_ending_the_answer()
    {
        // The toolset answers "no such tool" as a result, not an exception, so
        // the model can correct itself inside the same question.
        var client = new ScriptedAssistantClient(
            Calls("read_wages"),
            Says("Nemam pristup toj informaciji."));

        var answer = await Handler(client, new ScriptedToolset()).Handle(Ask(), default);

        Assert.Equal("Nemam pristup toj informaciji.", answer.Text);
        Assert.True(client.Provided[0][0].IsError);
    }

    [Fact]
    public async Task The_model_is_offered_only_what_the_toolset_allows()
    {
        var client = new ScriptedAssistantClient(Says("ok"));
        var toolset = new ScriptedToolset();

        await Handler(client, toolset).Handle(Ask(), default);

        Assert.Equal(
            toolset.Offered.Select(t => t.Name),
            client.StartedWith!.Tools.Select(t => t.Name));
    }

    [Fact]
    public async Task Earlier_turns_are_replayed_and_the_new_question_is_sent()
    {
        var client = new ScriptedAssistantClient(Says("ok"));

        var command = Ask("A u avgustu?") with
        {
            History =
            [
                new AssistantMessage(AssistantRole.User, "Koliko sati u julu?"),
                new AssistantMessage(AssistantRole.Assistant, "168."),
            ],
        };

        await Handler(client, new ScriptedToolset()).Handle(command, default);

        Assert.Equal(2, client.StartedWith!.History.Count);
        Assert.Equal(new[] { "A u avgustu?" }, client.Sent);
    }

    [Theory]
    [InlineData("sr", "Serbian")]
    [InlineData("en", "English")]
    public async Task The_prompt_names_the_language_to_answer_in(string locale, string expected)
    {
        var client = new ScriptedAssistantClient(Says("ok"));

        await Handler(client, new ScriptedToolset())
            .Handle(Ask() with { Locale = locale }, default);

        Assert.Contains(expected, client.StartedWith!.SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_prompt_names_who_is_asking_and_what_they_are()
    {
        // Without the role the model cannot say "you would need a project
        // manager for that", and without the date "this month" means nothing.
        var client = new ScriptedAssistantClient(Says("ok"));
        var user = new StubCurrentUser { Email = "ana@construction.local", Role = UserRole.Foreman };

        await Handler(client, new ScriptedToolset(), user).Handle(Ask(), default);

        var prompt = client.StartedWith!.SystemPrompt;

        Assert.Contains("ana@construction.local", prompt, StringComparison.Ordinal);
        Assert.Contains("Foreman", prompt, StringComparison.Ordinal);
        Assert.Contains("2026-07-31", prompt, StringComparison.Ordinal);
    }
}
