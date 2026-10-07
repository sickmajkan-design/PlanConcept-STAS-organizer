using Construction.API.Authorization;
using Construction.Application.Features.DataQuality;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>Records that need tidying, grouped by what is wrong with them.</summary>
[Authorize(Policy = Policies.AdminAndAbove)]
public class DataQualityController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/data-quality")]
    [HttpGet("/api/data-quality")]
    [ProducesResponseType(typeof(DataQualityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataQualityDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetDataQualityQuery(), cancellationToken));
    }
}
