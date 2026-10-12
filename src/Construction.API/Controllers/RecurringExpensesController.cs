using Construction.API.Authorization;
using Construction.Application.Features.Recurring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Costs that come back on their own, kept as templates from which entries of the book are made each period.
/// </summary>
[Authorize(Policy = Policies.AllEmployees)]
[Route("api/v{version:apiVersion}/book/recurring")]
[Route("api/book/recurring")]
public class RecurringExpensesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RecurringExpenseDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetRecurringExpensesQuery(), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecurringExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RecurringExpenseDto>> Create(
        CreateRecurringExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var created = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), null, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecurringExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RecurringExpenseDto>> Update(
        Guid id,
        UpdateRecurringExpenseCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(typeof(RecurringExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringExpenseDto>> Toggle(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new ToggleRecurringExpenseCommand(id), cancellationToken));
    }

    [HttpGet("preview")]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringPreviewRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RecurringPreviewRowDto>>> Preview(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetRecurringPreviewQuery(year, month), cancellationToken));
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<int>> Generate(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GenerateRecurringExpensesCommand(year, month), cancellationToken));
    }
}
