using Construction.API.Authorization;
using Construction.Application.Features.StorageCleanup;
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

    /// <summary>The whole picture of the server: machine, application, database and what the platform leans on.</summary>
    [HttpGet("/api/v{version:apiVersion}/system/status")]
    [HttpGet("/api/system/status")]
    [ProducesResponseType(typeof(ServerStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerStatusDto>> Status(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetServerStatusQuery(), cancellationToken));
    }

    /// <summary>What could be cleaned off the server: files nothing points at, documents of deleted records, traces.</summary>
    [HttpGet("/api/v{version:apiVersion}/system/storage/cleanup")]
    [HttpGet("/api/system/storage/cleanup")]
    [ProducesResponseType(typeof(StorageCleanupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageCleanupDto>> Cleanup(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetStorageCleanupQuery(), cancellationToken));
    }

    /// <summary>Removes one kind of leftover for good. Irreversible; the panel asks for confirmation first.</summary>
    [HttpPost("/api/v{version:apiVersion}/system/storage/cleanup")]
    [HttpPost("/api/system/storage/cleanup")]
    [ProducesResponseType(typeof(StorageCleanupResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageCleanupResultDto>> RunCleanup(
        [FromBody] RunStorageCleanupRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new RunStorageCleanupCommand(request.Kind), cancellationToken));
    }
}

/// <summary>Which kind of leftover to remove.</summary>
public class RunStorageCleanupRequest
{
    public StorageCleanupKind Kind { get; init; }
}
