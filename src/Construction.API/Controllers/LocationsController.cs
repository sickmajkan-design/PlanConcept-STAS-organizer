using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Locations.Commands.ReportLocations;
using Construction.Application.Features.Locations.Models;
using Construction.Application.Features.Locations.Queries.GetCurrentLocations;
using Construction.Application.Features.Locations.Queries.GetLastLocation;
using Construction.Application.Features.Locations.Queries.GetLocationHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class LocationsController : ApiControllerBase
{
    /// <summary>
    /// Ingests a batch of GPS pings from the mobile app (sent every 60 seconds
    /// while logged in; batches let the app catch up after being offline).
    /// The employee identity comes from the JWT, never from the payload.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(int), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> Report(
        ReportLocationsCommand command,
        CancellationToken cancellationToken)
    {
        var stored = await Mediator.Send(command, cancellationToken);
        return Accepted(value: stored);
    }

    /// <summary>
    /// Latest known position of every employee, for the live map.
    /// </summary>
    /// <remarks>
    /// Paged, with a much larger page than a grid uses because the caller is
    /// drawing markers. Compare <c>totalCount</c> against the items returned: a
    /// truncated map has no scrollbar to hint that somebody is missing.
    /// </remarks>
    [HttpGet("current")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<EmployeeLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<EmployeeLocationDto>>> Current(
        [FromQuery] GetCurrentLocationsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Last known position of one employee.</summary>
    [HttpGet("employees/{employeeId:guid}/last")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(EmployeeLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeLocationDto>> Last(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetEmployeeLastLocationQuery(employeeId), cancellationToken));
    }

    /// <summary>Paged movement history for one employee, newest first.</summary>
    [HttpGet("employees/{employeeId:guid}/history")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<LocationRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedList<LocationRecordDto>>> History(
        Guid employeeId,
        [FromQuery] GetLocationHistoryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query with { EmployeeId = employeeId }, cancellationToken));
    }
}
