using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.TimeEntries.Commands.ClockIn;
using Construction.Application.Features.TimeEntries.Commands.ClockOut;
using Construction.Application.Features.TimeEntries.Commands.CreateTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.DeleteTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.ReviewTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.UpdateTimeEntry;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Application.Features.TimeEntries.Queries.GetCurrentTimeEntry;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntries;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntryById;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntrySummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Work time. The read endpoints are open to every signed-in employee because
/// a worker needs their own timesheet; the handlers narrow the result to the
/// caller's own rows rather than the route refusing them outright.
/// </summary>
public class TimeEntriesController : ApiControllerBase
{
    /// <summary>Lists time entries. Workers see only their own.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(PagedList<TimeEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<TimeEntryDto>>> GetList(
        [FromQuery] GetTimeEntriesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Hours per employee for a period. Workers see only their own.</summary>
    [HttpGet("summary")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(TimeEntrySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TimeEntrySummaryDto>> GetSummary(
        [FromQuery] GetTimeEntrySummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// The caller's running shift, or 204 when they are not clocked in.
    /// </summary>
    [HttpGet("current")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TimeEntryDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var entry = await Mediator.Send(new GetCurrentTimeEntryQuery(), cancellationToken);

        // Being off shift is an ordinary answer, not a missing resource.
        return entry is null ? NoContent() : Ok(entry);
    }

    /// <summary>Returns one time entry. Workers may only read their own.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimeEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetTimeEntryByIdQuery(id), cancellationToken));
    }

    /// <summary>Starts the caller's shift.</summary>
    [HttpPost("clock-in")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeEntryDto>> ClockIn(
        ClockInCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    /// <summary>Ends the caller's running shift and submits it for review.</summary>
    [HttpPost("clock-out")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeEntryDto>> ClockOut(
        ClockOutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Records a shift on someone's behalf.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeEntryDto>> Create(
        CreateTimeEntryCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    /// <summary>Corrects a recorded shift. Refused with 409 once approved.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeEntryDto>> Update(
        Guid id,
        UpdateTimeEntryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Signs a shift off, or sends it back with a reason. Never your own.
    /// </summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(TimeEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeEntryDto>> Review(
        Guid id,
        ReviewTimeEntryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes a time entry. Refused with 409 once approved.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteTimeEntryCommand(id), cancellationToken);
        return NoContent();
    }
}
