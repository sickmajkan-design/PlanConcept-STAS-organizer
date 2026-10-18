using Construction.API.Authorization;
using Construction.Application.Features.Dashboard.Commands;
using Construction.Application.Features.Dashboard.Models;
using Construction.Application.Features.Dashboard.Queries;
using Construction.Application.Features.Dashboard.Snapshots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The signed-in user's own configurable home-dashboard layout. Admin and
/// SuperAdmin only — every other role still gets the static home page, so
/// there is nothing here for them to read or write.
/// </summary>
public class DashboardController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/dashboard-layout")]
    [HttpGet("/api/dashboard-layout")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(DashboardLayoutDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardLayoutDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetDashboardLayoutQuery(), cancellationToken));
    }

    [HttpPut("/api/v{version:apiVersion}/dashboard-layout")]
    [HttpPut("/api/dashboard-layout")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(DashboardLayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardLayoutDto>> Save(
        SaveDashboardLayoutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>What the board showed today and yesterday, for the "since yesterday" notes. Any staff account: the phone's Today list uses it too.</summary>
    [HttpGet("/api/v{version:apiVersion}/dashboard-snapshots")]
    [HttpGet("/api/dashboard-snapshots")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(DashboardSnapshotsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSnapshotsDto>> GetSnapshots(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetDashboardSnapshotsQuery(), cancellationToken));
    }

    [HttpPut("/api/v{version:apiVersion}/dashboard-snapshots")]
    [HttpPut("/api/dashboard-snapshots")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordSnapshots(
        RecordDashboardSnapshotsCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
