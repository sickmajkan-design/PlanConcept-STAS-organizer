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

    /// <summary>Who has clocked in today and how far from their site they are: the part of the board that changes by the minute.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/attendance")]
    [HttpGet("/api/planning/attendance")]
    [ProducesResponseType(typeof(PlanningAttendanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanningAttendanceDto>> Attendance([FromQuery] GetPlanningAttendanceQuery query, CancellationToken cancellationToken)
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

    /// <summary>Who held each vehicle and tool on one day (past, today or planned), and the changes still waiting for their day.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/equipment")]
    [HttpGet("/api/planning/equipment")]
    [ProducesResponseType(typeof(PlanningEquipmentOnDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanningEquipmentOnDto>> EquipmentOn([FromQuery] GetPlanningEquipmentQuery query, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Takes back a planned change of equipment before its day.</summary>
    [HttpDelete("/api/v{version:apiVersion}/planning/equipment/planned/{id:guid}")]
    [HttpDelete("/api/planning/equipment/planned/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPlannedEquipment(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new CancelPlannedEquipmentChangeCommand(id), cancellationToken);

        return NoContent();
    }

    /// <summary>What to do with equipment when someone leaves a site or is away: keep, hand over or return to the store.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/equipment")]
    [HttpPost("/api/planning/equipment")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> DecideEquipment(DecideEquipmentCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
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

    /// <summary>Hours the schedule planned for each site against the hours people reported, for a period.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/hours")]
    [HttpGet("/api/planning/hours")]
    [ProducesResponseType(typeof(PlannedHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlannedHoursDto>> Hours([FromQuery] GetPlannedHoursQuery query, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts one week of the schedule on another: the free people go where they were. With <c>dryRun</c> it only says what would happen.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/copy-week")]
    [HttpPost("/api/planning/copy-week")]
    [ProducesResponseType(typeof(WeekApplyResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WeekApplyResultDto>> CopyWeek(CopyScheduleWeekCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>The saved week templates.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/templates")]
    [HttpGet("/api/planning/templates")]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduleTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScheduleTemplateDto>>> Templates(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetScheduleTemplatesQuery(), cancellationToken));
    }

    /// <summary>Saves a week of the schedule as a template.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/templates")]
    [HttpPost("/api/planning/templates")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> SaveTemplate(SaveScheduleTemplateCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Puts a saved template on a week. With <c>dryRun</c> it only says what would happen.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/templates/apply")]
    [HttpPost("/api/planning/templates/apply")]
    [ProducesResponseType(typeof(WeekApplyResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeekApplyResultDto>> ApplyTemplate(ApplyScheduleTemplateCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpDelete("/api/v{version:apiVersion}/planning/templates/{id:guid}")]
    [HttpDelete("/api/planning/templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteScheduleTemplateCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>The crews: named groups of workers that are moved together.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/crews")]
    [HttpGet("/api/planning/crews")]
    [ProducesResponseType(typeof(IReadOnlyList<CrewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrewDto>>> Crews(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetCrewsQuery(), cancellationToken));
    }

    /// <summary>Creates a crew, or replaces the name, leader and members of one.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/crews")]
    [HttpPost("/api/planning/crews")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> SaveCrew(SaveCrewCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpDelete("/api/v{version:apiVersion}/planning/crews/{id:guid}")]
    [HttpDelete("/api/planning/crews/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCrew(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteCrewCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Puts every member of a crew on a site for a range of days.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/crews/assign")]
    [HttpPost("/api/planning/crews/assign")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> AssignCrew(AssignCrewCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Publishes a day: everybody posted that day is told where they go. The schedule itself does not change.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/publish")]
    [HttpPost("/api/planning/publish")]
    [ProducesResponseType(typeof(SchedulePublicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SchedulePublicationDto>> Publish(PublishScheduleDayCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>When each day of a period was last published.</summary>
    [HttpGet("/api/v{version:apiVersion}/planning/publications")]
    [HttpGet("/api/planning/publications")]
    [ProducesResponseType(typeof(IReadOnlyList<SchedulePublicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SchedulePublicationDto>>> Publications([FromQuery] GetSchedulePublicationsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Reminds the people posted on a day who have not yet confirmed it.</summary>
    [HttpPost("/api/v{version:apiVersion}/planning/remind")]
    [HttpPost("/api/planning/remind")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> Remind(RemindUnconfirmedPostingsCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
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
