using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Absences.Commands.DeleteAbsence;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Commands.ReviewAbsence;
using Construction.Application.Features.Absences.Models;
using Construction.Application.Features.Absences.Queries.GetAbsences;
using Construction.Application.Features.Absences.Queries.GetSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Time off, and the schedule it feeds.
/// </summary>
/// <remarks>
/// Open to every signed-in employee at the route because a worker books their
/// own leave. What each role may see and grant lives in
/// <see cref="Application.Features.Absences.AbsenceRules"/>.
/// </remarks>
public class AbsencesController : ApiControllerBase
{
    /// <summary>Lists absences. Workers see only their own.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(PagedList<AbsenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<AbsenceDto>>> GetList(
        [FromQuery] GetAbsencesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// Who is posted where, and who is away, over a window. A worker gets
    /// their own line, which is what the phone shows.
    /// </summary>
    [HttpGet("/api/schedule")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(
        [FromQuery] GetScheduleQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Books time off, or records it for somebody else.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(AbsenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceDto>> Book(
        RequestAbsenceCommand command,
        CancellationToken cancellationToken)
    {
        var absence = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = absence.Id }, absence);
    }

    /// <summary>Grants or refuses a request. Never your own.</summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(AbsenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceDto>> Review(
        Guid id,
        ReviewAbsenceCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Withdraws your own unanswered request, or removes one outright.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAbsenceCommand(id), cancellationToken);
        return NoContent();
    }
}
