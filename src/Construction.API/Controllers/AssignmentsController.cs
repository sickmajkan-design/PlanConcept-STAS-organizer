using Construction.API.Authorization;
using Construction.Application.Features.Assignments.Models;
using Construction.Application.Features.Assignments.Queries.GetAssignmentBoard;
using Construction.Application.Features.Assignments.Queries.GetAssignmentSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The drag-and-drop board: every employee against every open site, in one
/// call. The actual assigning and removing still go through the endpoints on
/// <see cref="EmployeesController"/> — this only reads.
/// </summary>
[Authorize(Policy = Policies.ProjectManagerAndAbove)]
public class AssignmentsController : ApiControllerBase
{
    /// <summary>Everyone available to staff, every open site, and who is on which.</summary>
    [HttpGet("/api/v{version:apiVersion}/assignment-board")]
    [HttpGet("/api/assignment-board")]
    [ProducesResponseType(typeof(AssignmentBoardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentBoardDto>> GetBoard(
        [FromQuery] GetAssignmentBoardQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// The planning board: every active employee with dated postings, approved
    /// absences, held equipment and today's status, for one to four weeks.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/assignment-board/schedule")]
    [HttpGet("/api/assignment-board/schedule")]
    [ProducesResponseType(typeof(AssignmentScheduleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentScheduleDto>> GetSchedule(
        [FromQuery] GetAssignmentScheduleQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}
