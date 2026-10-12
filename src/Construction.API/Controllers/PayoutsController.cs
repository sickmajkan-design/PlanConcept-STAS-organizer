using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Features.Payouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// What workers are paid out for a month: advances and the final payment, per group, against what the
/// group earned in the month's settlement.
/// </summary>
/// <remarks>
/// Pay: management only, with the full finance grant. The handlers enforce both; the route policy is the
/// office role.
/// </remarks>
[Authorize(Policy = Policies.AdminAndAbove)]
public class PayoutsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PayoutsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PayoutsDto>> Get(
        [FromQuery] GetPayoutsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(WagePayoutDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WagePayoutDto>> Create(
        CreateWagePayoutCommand command,
        CancellationToken cancellationToken)
    {
        var payout = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(Get), new { year = payout.Year, month = payout.Month }, payout);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WagePayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WagePayoutDto>> Update(
        Guid id,
        UpdateWagePayoutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteWagePayoutCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(WagePayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WagePayoutDto>> MarkPaid(
        Guid id,
        MarkWagePayoutPaidCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }
}
