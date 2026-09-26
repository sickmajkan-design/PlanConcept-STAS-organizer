using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Refunds.Commands.CreateRefund;
using Construction.Application.Features.Refunds.Commands.ReviewRefund;
using Construction.Application.Features.Refunds.Models;
using Construction.Application.Features.Refunds.Queries.GetRefunds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Requests to be paid back for something bought for the firm, with the receipt and the reason.
/// Approved ones are paid with the payroll of the month they name.
/// </summary>
/// <remarks>
/// Open to every signed-in employee at the route, because anyone may ask. Who decides, and what
/// each role may see, lives in <see cref="Application.Features.Refunds.RefundRules"/>.
/// </remarks>
public class RefundsController : ApiControllerBase
{
    /// <summary>Lists requests. The office sees all; everyone else what they asked for.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(PagedList<RefundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<RefundDto>>> GetList(
        [FromQuery] GetRefundsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Asks to be paid back.</summary>
    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(RefundDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RefundDto>> Create(
        CreateRefundCommand command,
        CancellationToken cancellationToken)
    {
        var refund = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = refund.Id }, refund);
    }

    /// <summary>Approves, declines or withdraws a request.</summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(RefundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RefundDto>> Review(
        Guid id,
        ReviewRefundCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }
}
