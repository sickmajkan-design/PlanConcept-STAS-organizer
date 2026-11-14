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

    /// <summary>Unexpected server errors of the last seven days, grouped by kind. Counts only, no messages.</summary>
    [HttpGet("/api/v{version:apiVersion}/system/errors")]
    [HttpGet("/api/system/errors")]
    [ProducesResponseType(typeof(List<ServerErrorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServerErrorDto>>> Errors(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetServerErrorsQuery(), cancellationToken));
    }

    /// <summary>Where each kind of server trouble is announced (e-mail, push) and when it last was.</summary>
    [HttpGet("/api/v{version:apiVersion}/system/alerts")]
    [HttpGet("/api/system/alerts")]
    [ProducesResponseType(typeof(List<ServerAlertSettingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServerAlertSettingDto>>> Alerts(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetServerAlertSettingsQuery(), cancellationToken));
    }

    /// <summary>Chooses the channels for one kind of server trouble.</summary>
    [HttpPut("/api/v{version:apiVersion}/system/alerts/{key}")]
    [HttpPut("/api/system/alerts/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAlert(string key, [FromBody] UpdateServerAlertRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UpdateServerAlertSettingCommand(key, request.EmailEnabled, request.PushEnabled), cancellationToken);
        return NoContent();
    }

    /// <summary>Sends one test notice to the signed-in Super Admin, by e-mail and push.</summary>
    [HttpPost("/api/v{version:apiVersion}/system/alerts/test")]
    [HttpPost("/api/system/alerts/test")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TestAlert(CancellationToken cancellationToken)
    {
        await Mediator.Send(new SendTestServerAlertCommand(), cancellationToken);
        return NoContent();
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

/// <summary>The channels chosen for one kind of server trouble.</summary>
public class UpdateServerAlertRequest
{
    public bool EmailEnabled { get; init; }

    public bool PushEnabled { get; init; }
}

/// <summary>Which kind of leftover to remove.</summary>
public class RunStorageCleanupRequest
{
    public StorageCleanupKind Kind { get; init; }
}
