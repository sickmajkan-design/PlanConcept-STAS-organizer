using Construction.API.Authorization;
using Construction.Application.Features.Setup.Queries.GetSetupChecklist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>What is still missing before the system does its job.</summary>
public class SetupController : ApiControllerBase
{
    /// <summary>The gaps, computed from live data. Empty when nothing is missing.</summary>
    [HttpGet("checklist")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(SetupChecklistDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupChecklistDto>> GetChecklist(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetSetupChecklistQuery(), cancellationToken));
    }
}
