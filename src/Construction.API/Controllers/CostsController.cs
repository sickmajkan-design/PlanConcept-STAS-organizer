using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Commands.DeleteCostRecord;
using Construction.Application.Features.Costs.Commands.RecordMaterialMovement;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Models;
using Construction.Application.Features.Costs.Queries.GetCostRecords;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
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
}
