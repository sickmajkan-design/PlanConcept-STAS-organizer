using Construction.API.Authorization;
using Construction.Application.Features.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// What clients still owe the firm: unpaid invoices less their part-payments, and incomes of the book that
/// are not marked paid, aged by due date.
/// </summary>
[Authorize(Policy = Policies.AllEmployees)]
public class ReceivablesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DebtsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DebtsDto>> Get(
        [FromQuery] GetReceivablesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}
