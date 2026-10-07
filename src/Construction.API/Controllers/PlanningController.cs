using Construction.API.Authorization;
using Construction.Application.Features.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The scheduling screen: who is posted where over any stretch of days, who is away, how many people
/// of which position each site needs, and the actions that move people around.
/// </summary>
/// <remarks>
/// Replaces the two older boards. Every action here sets a range of days outright rather than adding
/// to what is there, so a retried request changes nothing the first one did not.
/// </remarks>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class PlanningController : ApiControllerBase
{
    /// <summary>Postings, approved absences and needs for <c>[from, to]</c>.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning")]
    [HttpGet("/api/planning")]
    [ProducesResponseType(typeof(PlanningDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanningDto>> Get([FromQuery] GetPlanningQuery query, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// What the schedule will cost in labour, per site and month, beside what the approved hours have cost.
    /// Only for whoever may see what people are paid.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/planning/labour-cost")]
    [HttpGet("/api/planning/labour-cost")]
    [ProducesResponseType(typeof(PlannedLabourCostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlannedLabourCostDto>> LabourCost(
        [FromQuery] GetPlannedLabourCostQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Who changed the schedule and when, newest first.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/history")]
    [HttpGet("/api/planning/history")]
    [ProducesResponseType(typeof(IReadOnlyList<PlanningHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PlanningHistoryDto>>> History(
        [FromQuery] GetPlanningHistoryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts an employee on a project (or frees them) for a range of days.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/assign")]
    [HttpPost("/api/planning/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(SetEmployeeScheduleCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Swaps what two employees are doing over a range of days.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/swap")]
    [HttpPost("/api/planning/swap")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Swap(SwapEmployeeSchedulesCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Sets how many people of each position a project needs.</summary>
    [HttpPut("/api/v{version:apiVersion}/planning/projects/{projectId:guid}/needs")]
    [HttpPut("/api/planning/projects/{projectId:guid}/needs")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetNeeds(
        Guid projectId,
        [FromBody] SetNeedsRequest request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new SetProjectStaffingNeedsCommand { ProjectId = projectId, Needs = request.Needs, RequiredCertificates = request.RequiredCertificates },
            cancellationToken);
        return NoContent();
    }
}

/// <summary>The body <see cref="PlanningController.SetNeeds"/> takes.</summary>
public class SetNeedsRequest
{
    public List<StaffingNeedInput> Needs { get; set; } = [];

    /// <summary>Null leaves the project required certificates as they are.</summary>
    public List<string>? RequiredCertificates { get; set; }
}
