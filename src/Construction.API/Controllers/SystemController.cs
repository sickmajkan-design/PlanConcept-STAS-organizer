using Construction.API.Authorization;
using Construction.Application.Features.SystemStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>How the server is doing, for the person who runs it.</summary>
[Authorize(Policy = Policies.SuperAdminOnly)]
public class SystemController : ApiControllerBase
{
    /// <summary>Free and used disk space and how much of it the documents take.</summary>
    [HttpGet("/api/v{version:apiVersion}/system/storage")]
    [HttpGet("/api/system/storage")]
    [ProducesResponseType(typeof(StorageStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageStatusDto>> Storage(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetStorageStatusQuery(), cancellationToken));
    }
}
