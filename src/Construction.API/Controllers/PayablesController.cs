using Construction.API.Authorization;
using Construction.Application.Features.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>What the firm still owes: costs of the book that are not marked paid, aged by due date.</summary>
[Authorize(Policy = Policies.AllEmployees)]
public class PayablesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DebtsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DebtsDto>> Get(
        [FromQuery] GetPayablesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}
