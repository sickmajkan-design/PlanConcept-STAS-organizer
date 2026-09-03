using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.DeleteLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Commands.UpdateLedger;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerById;
using Construction.Application.Features.Ledgers.Queries.GetLedgers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The SuperAdmin's own free-form record-keeping — a monthly spreadsheet
/// with user-defined columns, laid out as sections (companies/sites) of rows
/// (usually workers). Nothing here is schema the rest of the app reads;
/// everything a SuperAdmin adds is theirs to shape.
/// </summary>
[Authorize(Policy = Policies.SuperAdminOnly)]
public class LedgersController : ApiControllerBase
{
    // ---- ledgers (months) --------------------------------------------------

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<LedgerSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<LedgerSummaryDto>>> GetList(
        [FromQuery] GetLedgersQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LedgerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetLedgerByIdQuery(id), cancellationToken));
    }

    /// <summary>Starts a new month, optionally copying another ledger's columns/sections/rows.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LedgerDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerDetailDto>> Create(
        CreateLedgerCommand command,
        CancellationToken cancellationToken)
    {
        var ledger = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = ledger.Id }, ledger);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LedgerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerDetailDto>> Update(
        Guid id,
        UpdateLedgerCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteLedgerCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- columns -------------------------------------------------------------

    [HttpPost("{ledgerId:guid}/columns")]
    [ProducesResponseType(typeof(LedgerColumnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerColumnDto>> AddColumn(
        Guid ledgerId,
        AddLedgerColumnCommand command,
        CancellationToken cancellationToken)
    {
        var column = await Mediator.Send(command with { LedgerId = ledgerId }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ledgerId }, column);
    }

    [HttpPut("columns/{columnId:guid}")]
    [ProducesResponseType(typeof(LedgerColumnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerColumnDto>> UpdateColumn(
        Guid columnId,
        UpdateLedgerColumnCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = columnId }, cancellationToken));
    }

    [HttpDelete("columns/{columnId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteColumn(Guid columnId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteLedgerColumnCommand(columnId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{ledgerId:guid}/columns/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderColumns(
        Guid ledgerId,
        ReorderLedgerColumnsCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { LedgerId = ledgerId }, cancellationToken);
        return NoContent();
    }

    // ---- sections --------------------------------------------------------

    [HttpPost("{ledgerId:guid}/sections")]
    [ProducesResponseType(typeof(LedgerSectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerSectionDto>> AddSection(
        Guid ledgerId,
        AddLedgerSectionCommand command,
        CancellationToken cancellationToken)
    {
        var section = await Mediator.Send(command with { LedgerId = ledgerId }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ledgerId }, section);
    }

    [HttpPut("sections/{sectionId:guid}")]
    [ProducesResponseType(typeof(LedgerSectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerSectionDto>> UpdateSection(
        Guid sectionId,
        UpdateLedgerSectionCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = sectionId }, cancellationToken));
    }

    [HttpDelete("sections/{sectionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSection(Guid sectionId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteLedgerSectionCommand(sectionId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{ledgerId:guid}/sections/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderSections(
        Guid ledgerId,
        ReorderLedgerSectionsCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { LedgerId = ledgerId }, cancellationToken);
        return NoContent();
    }

    // ---- rows --------------------------------------------------------------

    [HttpPost("sections/{sectionId:guid}/rows")]
    [ProducesResponseType(typeof(LedgerRowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerRowDto>> AddRow(
        Guid sectionId,
        AddLedgerRowCommand command,
        CancellationToken cancellationToken)
    {
        var row = await Mediator.Send(command with { SectionId = sectionId }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sectionId }, row);
    }

    [HttpPut("rows/{rowId:guid}")]
    [ProducesResponseType(typeof(LedgerRowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerRowDto>> UpdateRow(
        Guid rowId,
        UpdateLedgerRowCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = rowId }, cancellationToken));
    }

    [HttpDelete("rows/{rowId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRow(Guid rowId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteLedgerRowCommand(rowId), cancellationToken);
        return NoContent();
    }

    [HttpPut("sections/{sectionId:guid}/rows/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderRows(
        Guid sectionId,
        ReorderLedgerRowsCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { SectionId = sectionId }, cancellationToken);
        return NoContent();
    }

    // ---- cells ---------------------------------------------------------------

    /// <summary>Sets one cell's value. Called on every edit made to the grid.</summary>
    [HttpPut("cells")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCell(SetLedgerCellCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
