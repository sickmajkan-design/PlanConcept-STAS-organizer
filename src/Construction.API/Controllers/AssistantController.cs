using Construction.API.Authorization;
using Construction.API.Extensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Controllers;

/// <summary>
/// The office assistant: questions about the company's own records, answered
/// in the language they were asked in.
/// </summary>
/// <remarks>
/// <para>
/// Read-only. The assistant has no tool that writes, and the endpoints below
/// add no path to the data that a controller does not already guard — every
/// tool is an existing query, run behind the policy its own controller
/// carries. See <see cref="Assistant.AssistantToolset"/>.
/// </para>
/// <para>
/// Restricted to Foreman and above because that is who the admin panel is
/// for. It is a narrowing, not a control: a Worker who reached this would
/// still only be offered the tools their role allows, and those queries
/// narrow themselves to their own rows.
/// </para>
/// </remarks>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class AssistantController : ApiControllerBase
{
    private readonly IAssistantClient _client;

    public AssistantController(IAssistantClient client)
    {
        _client = client;
    }

    /// <summary>Whether this installation has the assistant configured at all.</summary>
    /// <remarks>
    /// The panel asks first and hides itself when the answer is no, so an
    /// installation without an API key shows no button rather than a button
    /// that fails.
    /// </remarks>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AssistantStatusDto), StatusCodes.Status200OK)]
    public ActionResult<AssistantStatusDto> GetStatus()
    {
        return Ok(new AssistantStatusDto(_client.IsConfigured));
    }

    /// <summary>Asks a question and returns the answer.</summary>
    [HttpPost("chat")]
    [EnableRateLimiting(RateLimitingExtensions.AssistantPolicy)]
    [ProducesResponseType(typeof(AssistantAnswer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AssistantAnswer>> Chat(
        AskAssistantCommand command,
        CancellationToken cancellationToken)
    {
        if (!_client.IsConfigured)
        {
            throw new ServiceUnavailableException(
                "The assistant is not configured on this installation.");
        }

        return Ok(await Mediator.Send(command, cancellationToken));
    }
}

/// <param name="Enabled">False when no API key is configured.</param>
public sealed record AssistantStatusDto(bool Enabled);
