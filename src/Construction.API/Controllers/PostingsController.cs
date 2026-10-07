using Construction.API.Authorization;
using Construction.Application.Features.Postings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>What a worker does with the postings they are given.</summary>
[Authorize(Policy = Policies.AllEmployees)]
public class PostingsController : ApiControllerBase
{
    /// <summary>Confirms the caller's own posting. Safe to repeat.</summary>
    [HttpPost("/api/v{version:apiVersion}/postings/{id:guid}/acknowledge")]
    [HttpPost("/api/postings/{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AcknowledgePostingCommand(id), cancellationToken);
        return NoContent();
    }
}
