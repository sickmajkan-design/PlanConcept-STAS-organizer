using Construction.API.Authorization;
using Construction.Application.Features.ScheduledReports.Commands.CreateScheduledReportSubscription;
using Construction.Application.Features.ScheduledReports.Commands.DeleteScheduledReportSubscription;
using Construction.Application.Features.ScheduledReports.Models;
using Construction.Application.Features.ScheduledReports.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// "Email me this export every week/month" subscriptions — Admin and above,
/// same tier as the exports they re-run.
/// </summary>
[Authorize(Policy = Policies.AdminAndAbove)]
public class ScheduledReportsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduledReportSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScheduledReportSubscriptionDto>>> GetList(
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetScheduledReportSubscriptionsQuery(), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScheduledReportSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduledReportSubscriptionDto>> Create(
        CreateScheduledReportSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var subscription = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), subscription);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteScheduledReportSubscriptionCommand(id), cancellationToken);
        return NoContent();
    }
}
