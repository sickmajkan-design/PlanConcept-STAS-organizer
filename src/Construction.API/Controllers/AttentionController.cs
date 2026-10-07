using Construction.API.Authorization;
using Construction.Application.Features.Attention;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>What is waiting for the caller to act on, limited to what their role may act on.</summary>
[Authorize(Policy = Policies.AllEmployees)]
public class AttentionController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/attention")]
    [HttpGet("/api/attention")]
    [ProducesResponseType(typeof(AttentionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttentionDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetAttentionQuery(), cancellationToken));
    }
}
