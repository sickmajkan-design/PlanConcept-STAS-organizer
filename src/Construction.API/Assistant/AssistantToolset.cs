using System.Text.Json;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Construction.API.Assistant;

/// <summary>
/// Runs the assistant's tools as the signed-in user, and refuses the rest.
/// </summary>
/// <remarks>
/// <para>
/// This class exists because of one fact about the codebase: role
/// authorization lives on controller actions, and the MediatR pipeline has no
/// authorization behaviour. A tool that called <c>IMediator.Send</c> directly
/// would therefore run a Project Manager's query for a Foreman with nothing in
/// the way. Every call goes past <see cref="IAuthorizationService"/> here
/// instead, against the policy copied from the query's own controller.
/// </para>
/// <para>
/// Row-level scoping needs nothing added: the assistant runs inside the
/// caller's own HTTP request, so <c>ICurrentUserService</c> resolves to the
/// real person and <c>TimeEntryAccess</c>, <c>CrewVisibility</c> and the rest
/// narrow their queries exactly as they do for the panel. For the same reason
/// anything the assistant ever writes is attributed to the real user by the
/// audit interceptor rather than to a service account.
/// </para>
/// </remarks>
public sealed class AssistantToolset : IAssistantToolset
{
    private readonly ISender _mediator;
    private readonly IAuthorizationService _authorization;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AssistantToolset> _logger;

    public AssistantToolset(
        ISender mediator,
        IAuthorizationService authorization,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AssistantToolset> logger)
    {
        _mediator = mediator;
        _authorization = authorization;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AssistantToolDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default)
    {
        List<AssistantToolDescriptor> allowed = [];

        foreach (var tool in AssistantToolRegistry.All)
        {
            if (await IsAllowedAsync(tool))
            {
                allowed.Add(new AssistantToolDescriptor(
                    tool.Name,
                    tool.Description,
                    tool.InputSchemaJson));
            }
        }

        return allowed;
    }

    public async Task<AssistantToolOutcome> InvokeAsync(
        AssistantToolCall call,
        CancellationToken cancellationToken = default)
    {
        var tool = AssistantToolRegistry.Find(call.Name);

        if (tool is null)
        {
            // A name the model invented. Reported back rather than thrown, so
            // it can correct itself instead of the whole answer failing.
            return Refused(call, $"There is no tool called '{call.Name}'.");
        }

        // Checked again even though DescribeAsync already filtered. That call
        // is a convenience for the model; this one is the control, and it is
        // the only one that would still be here if the description were ever
        // built somewhere else or cached across users.
        if (!await IsAllowedAsync(tool))
        {
            _logger.LogWarning(
                "Assistant refused tool {ToolName}: the caller does not hold {Policy}",
                tool.Name,
                tool.RequiredPolicy);

            return Refused(call, $"You are not permitted to use '{call.Name}'.");
        }

        try
        {
            var result = await tool.InvokeAsync(call.ArgumentsJson, _mediator, cancellationToken);

            return new AssistantToolOutcome(call.Id, AssistantRedactor.Serialise(result), false);
        }
        catch (Exception exception) when (
            exception is ValidationException
                or NotFoundException
                or ForbiddenAccessException
                or ConflictException
                or JsonException)
        {
            // The model asked for something that does not exist, or asked
            // badly. Both are recoverable within the conversation — a page
            // size of 5000 or a reversed date range should come back as
            // "that was refused, and why", not as a failed answer.
            _logger.LogInformation(
                exception,
                "Assistant tool {ToolName} was refused by the application layer",
                tool.Name);

            return Refused(call, exception.Message);
        }
    }

    private static AssistantToolOutcome Refused(AssistantToolCall call, string reason) =>
        new(call.Id, reason, true);

    private async Task<bool> IsAllowedAsync(AssistantTool tool)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            // Fails closed. Reached only if the toolset is ever resolved
            // outside an authenticated request, which is exactly when
            // guessing would be worst.
            return false;
        }

        var result = await _authorization.AuthorizeAsync(user, tool.RequiredPolicy);

        return result.Succeeded;
    }
}
