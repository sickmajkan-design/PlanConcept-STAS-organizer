using Construction.API.Authorization;
using Construction.Application.Features.BookImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Brings a month of the spreadsheet the firm kept before into the monthly settlement. The browser reads the
/// file; the server checks it row by row, shows what it would do, and only then writes.
/// </summary>
/// <remarks>Super Admin only: it writes whole months, with prices and pay.</remarks>
[Authorize(Policy = Policies.SuperAdminOnly)]
[Route("api/v{version:apiVersion}/book/imports")]
[Route("api/book/imports")]
public class BookImportsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookImportBatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookImportBatchDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetBookImportsQuery(), cancellationToken));
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(BookImportPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookImportPlanDto>> Preview(
        BookImportInput input,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new PreviewBookImportCommand(input), cancellationToken));
    }

    [HttpPost("commit")]
    [ProducesResponseType(typeof(BookImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookImportResultDto>> Commit(
        BookImportInput input,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new CommitBookImportCommand(input), cancellationToken));
    }

    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Undo(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UndoBookImportCommand(id), cancellationToken);
        return NoContent();
    }
}
