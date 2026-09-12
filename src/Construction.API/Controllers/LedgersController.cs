using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.DeleteLedger;
using Construction.Application.Features.Ledgers.Commands.PromoteLedgerRow;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Commands.SetLedgerColor;
using Construction.Application.Features.Ledgers.Commands.UpdateLedger;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerById;
using Construction.Application.Features.Ledgers.Queries.GetLedgerPromotions;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSectionRows;
using Construction.Application.Features.Ledgers.Queries.GetLedgerUnlinkedRows;
using Construction.Application.Features.Ledgers.Queries.GetLedgers;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSummary;
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

    /// <summary>One section's rows and cells — fetched only once a section is opened.</summary>
    [HttpGet("{ledgerId:guid}/sections/{sectionId:guid}/rows")]
    [ProducesResponseType(typeof(LedgerSectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerSectionDto>> GetSectionRows(
        Guid ledgerId,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetLedgerSectionRowsQuery(ledgerId, sectionId), cancellationToken));
    }

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

    // ---- promote to a real feature ------------------------------------------

    /// <summary>Pushes a manually-typed row through the real General Expense form.</summary>
    [HttpPost("rows/{rowId:guid}/promote/general-expense")]
    [ProducesResponseType(typeof(LedgerRowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LedgerRowDto>> PromoteRowToGeneralExpense(
        Guid rowId,
        PromoteLedgerRowToGeneralExpenseCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { RowId = rowId }, cancellationToken));
    }

    /// <summary>Pushes a manually-typed row through the real Accommodation-rate form.</summary>
    [HttpPost("rows/{rowId:guid}/promote/accommodation-rate")]
    [ProducesResponseType(typeof(LedgerRowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LedgerRowDto>> PromoteRowToAccommodationRate(
        Guid rowId,
        PromoteLedgerRowToAccommodationRateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { RowId = rowId }, cancellationToken));
    }

    // ---- summary panel ---------------------------------------------------------

    /// <summary>Every summary box for one ledger, with its current computed value and the net total.</summary>
    [HttpGet("{ledgerId:guid}/summary")]
    [ProducesResponseType(typeof(LedgerSummaryPanelDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LedgerSummaryPanelDto>> GetSummary(
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetLedgerSummaryQuery(ledgerId), cancellationToken));
    }

    /// <summary>Every row in this ledger with no Employee/Vehicle/Tool/Material link.</summary>
    [HttpGet("{ledgerId:guid}/unlinked-rows")]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerUnlinkedRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LedgerUnlinkedRowDto>>> GetUnlinkedRows(
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetLedgerUnlinkedRowsQuery(ledgerId), cancellationToken));
    }

    /// <summary>Every row in this ledger already pushed through to a real expense/rate.</summary>
    [HttpGet("{ledgerId:guid}/promotions")]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerPromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LedgerPromotionDto>>> GetPromotions(
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetLedgerPromotionsQuery(ledgerId), cancellationToken));
    }

    [HttpPost("{ledgerId:guid}/summary-boxes")]
    [ProducesResponseType(typeof(LedgerSummaryBoxDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerSummaryBoxDto>> AddSummaryBox(
        Guid ledgerId,
        AddLedgerSummaryBoxCommand command,
        CancellationToken cancellationToken)
    {
        var box = await Mediator.Send(command with { LedgerId = ledgerId }, cancellationToken);
        return CreatedAtAction(nameof(GetSummary), new { ledgerId }, box);
    }

    [HttpPut("summary-boxes/{boxId:guid}")]
    [ProducesResponseType(typeof(LedgerSummaryBoxDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerSummaryBoxDto>> UpdateSummaryBox(
        Guid boxId,
        UpdateLedgerSummaryBoxCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = boxId }, cancellationToken));
    }

    [HttpDelete("summary-boxes/{boxId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSummaryBox(Guid boxId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteLedgerSummaryBoxCommand(boxId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{ledgerId:guid}/summary-boxes/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderSummaryBoxes(
        Guid ledgerId,
        ReorderLedgerSummaryBoxesCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { LedgerId = ledgerId }, cancellationToken);
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

    // ---- colors ----------------------------------------------------------

    /// <summary>Colors (or clears the color of) one whole row.</summary>
    [HttpPut("rows/{rowId:guid}/color")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRowColor(
        Guid rowId,
        SetLedgerRowColorCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { RowId = rowId }, cancellationToken);
        return NoContent();
    }

    /// <summary>Colors (or clears the color of) one cell, without touching its value.</summary>
    [HttpPut("cells/color")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCellColor(
        SetLedgerCellColorCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
