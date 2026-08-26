using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Commands.DeleteCostRecord;
using Construction.Application.Features.Costs.Commands.RecordFinanceEntry;
using Construction.Application.Features.Costs.Commands.RecordMaterialMovement;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Commands.RecordToolExpense;
using Construction.Application.Features.Costs.Commands.UpdateEmployeeRate;
using Construction.Application.Features.Costs.Commands.UpdateFinanceEntry;
using Construction.Application.Features.Costs.Commands.UpdateMaterialMovement;
using Construction.Application.Features.Costs.Commands.UpdateToolExpense;
using Construction.Application.Features.Costs.Commands.UpdateVehicleExpense;
using Construction.Application.Features.Costs.Models;
using Construction.Application.Features.Costs.Queries.GetCostRecords;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.Costs.Queries.GetToolCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// What things cost: pay rates, stock movements, vehicle expenses, and the
/// reports built from them.
/// </summary>
/// <remarks>
/// The route policy is <c>ForemanAndAbove</c> — the widest role that has any
/// business here at all — and the finer split lives in <see cref="CostRules"/>.
/// It has to, because this module does not follow the plain hierarchy the rest
/// of the system does: a foreman records a delivery and reads what their site
/// consumed, but pay rates stop at the office. A route attribute cannot say
/// "this endpoint, but not this column".
/// </remarks>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class CostsController : ApiControllerBase
{
    // ---- pay rates -------------------------------------------------------

    /// <summary>Lists pay rates. Refused below Project Manager.</summary>
    [HttpGet("/api/v{version:apiVersion}/employee-rates")]
    [HttpGet("/api/employee-rates")]
    [ProducesResponseType(typeof(PagedList<EmployeeRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<EmployeeRateDto>>> GetRates(
        [FromQuery] GetEmployeeRatesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and average of whatever the rates list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/employee-rates/summary")]
    [HttpGet("/api/employee-rates/summary")]
    [ProducesResponseType(typeof(EmployeeRateSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeRateSummaryDto>> GetRatesSummary(
        [FromQuery] GetEmployeeRatesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts a new rate in force, closing off the one before it.</summary>
    [HttpPost("/api/v{version:apiVersion}/employee-rates")]
    [HttpPost("/api/employee-rates")]
    [Idempotent]
    [ProducesResponseType(typeof(EmployeeRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeRateDto>> SetRate(
        SetEmployeeRateCommand command,
        CancellationToken cancellationToken)
    {
        var rate = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetRates), new { id = rate.Id }, rate);
    }

    /// <summary>Corrects a rate that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/employee-rates/{id:guid}")]
    [HttpPut("/api/employee-rates/{id:guid}")]
    [ProducesResponseType(typeof(EmployeeRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeRateDto>> UpdateRate(
        Guid id,
        UpdateEmployeeRateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a rate. Admin and above.</summary>
    [HttpDelete("/api/v{version:apiVersion}/employee-rates/{id:guid}")]
    [HttpDelete("/api/employee-rates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRate(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteEmployeeRateCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- stock movements -------------------------------------------------

    /// <summary>Lists deliveries, issues and corrections.</summary>
    [HttpGet("/api/v{version:apiVersion}/material-movements")]
    [HttpGet("/api/material-movements")]
    [ProducesResponseType(typeof(PagedList<MaterialMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<MaterialMovementDto>>> GetMovements(
        [FromQuery] GetMaterialMovementsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and value of whatever the movements list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/material-movements/summary")]
    [HttpGet("/api/material-movements/summary")]
    [ProducesResponseType(typeof(MaterialMovementSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MaterialMovementSummaryDto>> GetMovementsSummary(
        [FromQuery] GetMaterialMovementsSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records a delivery, an issue to site, or a correction.</summary>
    [HttpPost("/api/v{version:apiVersion}/material-movements")]
    [HttpPost("/api/material-movements")]
    [Idempotent]
    [ProducesResponseType(typeof(MaterialMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaterialMovementDto>> RecordMovement(
        RecordMaterialMovementCommand command,
        CancellationToken cancellationToken)
    {
        var movement = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetMovements), new { id = movement.Id }, movement);
    }

    /// <summary>Corrects a movement that was recorded wrong, adjusting the stock effect to match.</summary>
    [HttpPut("/api/v{version:apiVersion}/material-movements/{id:guid}")]
    [HttpPut("/api/material-movements/{id:guid}")]
    [ProducesResponseType(typeof(MaterialMovementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaterialMovementDto>> UpdateMovement(
        Guid id,
        UpdateMaterialMovementCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a movement and puts the stock back.</summary>
    [HttpDelete("/api/v{version:apiVersion}/material-movements/{id:guid}")]
    [HttpDelete("/api/material-movements/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMovement(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteMaterialMovementCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- vehicle expenses ------------------------------------------------

    /// <summary>Lists fuel, servicing and everything else a vehicle costs.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-expenses")]
    [HttpGet("/api/vehicle-expenses")]
    [ProducesResponseType(typeof(PagedList<VehicleExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<VehicleExpenseDto>>> GetVehicleExpenses(
        [FromQuery] GetVehicleExpensesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total of whatever the vehicle-expense list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-expenses/summary")]
    [HttpGet("/api/vehicle-expenses/summary")]
    [ProducesResponseType(typeof(VehicleExpenseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VehicleExpenseSummaryDto>> GetVehicleExpensesSummary(
        [FromQuery] GetVehicleExpensesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records a tank of fuel, a service, or another cost.</summary>
    [HttpPost("/api/v{version:apiVersion}/vehicle-expenses")]
    [HttpPost("/api/vehicle-expenses")]
    [Idempotent]
    [ProducesResponseType(typeof(VehicleExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleExpenseDto>> RecordVehicleExpense(
        RecordVehicleExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetVehicleExpenses), new { id = expense.Id }, expense);
    }

    /// <summary>Corrects a vehicle cost that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/vehicle-expenses/{id:guid}")]
    [HttpPut("/api/vehicle-expenses/{id:guid}")]
    [ProducesResponseType(typeof(VehicleExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleExpenseDto>> UpdateVehicleExpense(
        Guid id,
        UpdateVehicleExpenseCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a recorded cost.</summary>
    [HttpDelete("/api/v{version:apiVersion}/vehicle-expenses/{id:guid}")]
    [HttpDelete("/api/vehicle-expenses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicleExpense(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleExpenseCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- tool expenses -----------------------------------------------------

    /// <summary>Lists repairs, servicing and everything else a tool costs.</summary>
    [HttpGet("/api/v{version:apiVersion}/tool-expenses")]
    [HttpGet("/api/tool-expenses")]
    [ProducesResponseType(typeof(PagedList<ToolExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<ToolExpenseDto>>> GetToolExpenses(
        [FromQuery] GetToolExpensesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total of whatever the tool-expense list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/tool-expenses/summary")]
    [HttpGet("/api/tool-expenses/summary")]
    [ProducesResponseType(typeof(ToolExpenseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ToolExpenseSummaryDto>> GetToolExpensesSummary(
        [FromQuery] GetToolExpensesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records a repair, a service, or another cost of a tool.</summary>
    [HttpPost("/api/v{version:apiVersion}/tool-expenses")]
    [HttpPost("/api/tool-expenses")]
    [Idempotent]
    [ProducesResponseType(typeof(ToolExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolExpenseDto>> RecordToolExpense(
        RecordToolExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetToolExpenses), new { id = expense.Id }, expense);
    }

    /// <summary>Corrects a tool cost that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/tool-expenses/{id:guid}")]
    [HttpPut("/api/tool-expenses/{id:guid}")]
    [ProducesResponseType(typeof(ToolExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolExpenseDto>> UpdateToolExpense(
        Guid id,
        UpdateToolExpenseCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a recorded cost.</summary>
    [HttpDelete("/api/v{version:apiVersion}/tool-expenses/{id:guid}")]
    [HttpDelete("/api/tool-expenses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteToolExpense(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteToolExpenseCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- finance entries ---------------------------------------------------

    /// <summary>Lists pay entries. Refused below Project Manager.</summary>
    [HttpGet("/api/v{version:apiVersion}/finance-entries")]
    [HttpGet("/api/finance-entries")]
    [ProducesResponseType(typeof(PagedList<FinanceEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<FinanceEntryDto>>> GetFinanceEntries(
        [FromQuery] GetFinanceEntriesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total of whatever the finance-entries list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/finance-entries/summary")]
    [HttpGet("/api/finance-entries/summary")]
    [ProducesResponseType(typeof(FinanceEntrySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FinanceEntrySummaryDto>> GetFinanceEntriesSummary(
        [FromQuery] GetFinanceEntriesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records what an employee is owed for a stretch of work.</summary>
    [HttpPost("/api/v{version:apiVersion}/finance-entries")]
    [HttpPost("/api/finance-entries")]
    [Idempotent]
    [ProducesResponseType(typeof(FinanceEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinanceEntryDto>> RecordFinanceEntry(
        RecordFinanceEntryCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetFinanceEntries), new { id = entry.Id }, entry);
    }

    /// <summary>Corrects a pay entry that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/finance-entries/{id:guid}")]
    [HttpPut("/api/finance-entries/{id:guid}")]
    [ProducesResponseType(typeof(FinanceEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinanceEntryDto>> UpdateFinanceEntry(
        Guid id,
        UpdateFinanceEntryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a pay entry.</summary>
    [HttpDelete("/api/v{version:apiVersion}/finance-entries/{id:guid}")]
    [HttpDelete("/api/finance-entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFinanceEntry(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteFinanceEntryCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- the reports -----------------------------------------------------

    /// <summary>
    /// What each site cost. Below Project Manager the labour half comes back
    /// as zero rather than the whole report being refused.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/costs/projects")]
    [HttpGet("/api/costs/projects")]
    [ProducesResponseType(typeof(ProjectCostReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectCostReportDto>> GetProjectCosts(
        [FromQuery] GetProjectCostsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>What the fleet cost, and what it drank.</summary>
    [HttpGet("/api/v{version:apiVersion}/costs/vehicles")]
    [HttpGet("/api/costs/vehicles")]
    [ProducesResponseType(typeof(VehicleCostReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VehicleCostReportDto>> GetVehicleCosts(
        [FromQuery] GetVehicleCostsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>What the tool fleet cost.</summary>
    [HttpGet("/api/v{version:apiVersion}/costs/tools")]
    [HttpGet("/api/costs/tools")]
    [ProducesResponseType(typeof(ToolCostReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ToolCostReportDto>> GetToolCosts(
        [FromQuery] GetToolCostsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}
