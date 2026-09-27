using Construction.API.Authorization;
using Construction.Application.Features.SignedTimesheets.Commands.GetOrCreateSignedTimesheet;
using Construction.Application.Features.SignedTimesheets.Queries.GetSignedTimesheetWeeks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The (project, calendar week) rows a client's signed timesheet scan is filed against.
/// SuperAdmin only, the same door as <see cref="LedgersController"/>: this is the
/// monthly payroll's own source document, not site paperwork.
/// </summary>
[Authorize(Policy = Policies.SuperAdminOnly)]
public class SignedTimesheetsController : ApiControllerBase
{
    /// <summary>The weeks one project/month touches, each flagged with whether it has a scan.</summary>
    [HttpGet("weeks")]
    [ProducesResponseType(typeof(IReadOnlyList<SignedTimesheetWeekDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SignedTimesheetWeekDto>>> GetWeeks(
        [FromQuery] GetSignedTimesheetWeeksQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Resolves a (project, week) pair to a row id, creating it the first time it is used.</summary>
    [HttpPost("get-or-create")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> GetOrCreate(
        [FromBody] GetOrCreateSignedTimesheetCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }
}
