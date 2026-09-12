using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Commands.DeleteCostRecord;
using Construction.Application.Features.Costs.Commands.RecordFinanceEntry;
using Construction.Application.Features.Costs.Commands.RecordGeneralExpense;
using Construction.Application.Features.Costs.Commands.RecordMaterialMovement;
using Construction.Application.Features.Costs.Commands.RecordToolRentalOut;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Commands.RecordVehicleRentalOut;
using Construction.Application.Features.Costs.Commands.ReturnToolRentalOut;
using Construction.Application.Features.Costs.Commands.ReturnVehicleRentalOut;
using Construction.Application.Features.Costs.Commands.SetAccommodationRate;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Commands.SetVehicleRentalRate;
using Construction.Application.Features.Costs.Commands.SetToolRentalRate;
using Construction.Application.Features.Costs.Commands.RecordToolExpense;
using Construction.Application.Features.Costs.Commands.UpdateAccommodationRate;
using Construction.Application.Features.Costs.Commands.UpdateEmployeeRate;
using Construction.Application.Features.Costs.Commands.UpdateGeneralExpense;
using Construction.Application.Features.Costs.Commands.UpdateFinanceEntry;
using Construction.Application.Features.Costs.Commands.UpdateMaterialMovement;
using Construction.Application.Features.Costs.Commands.UpdateToolExpense;
using Construction.Application.Features.Costs.Commands.UpdateToolRentalOut;
using Construction.Application.Features.Costs.Commands.UpdateToolRentalRate;
using Construction.Application.Features.Costs.Commands.UpdateVehicleExpense;
using Construction.Application.Features.Costs.Commands.UpdateVehicleRentalOut;
using Construction.Application.Features.Costs.Commands.UpdateVehicleRentalRate;
using Construction.Application.Features.Costs.Models;
using Construction.Application.Features.Costs.Queries.GetCostRecords;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.Costs.Queries.GetToolCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Application.Features.FuelCards.Commands;
using Construction.Application.Features.FuelCards.Import;
using Construction.Application.Features.FuelCards.Models;
using Construction.Application.Features.FuelCards.Queries;
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

    // ---- fuel cards --------------------------------------------------------

    /// <summary>Lists fuel cards, one per card ever issued against a vehicle.</summary>
    [HttpGet("/api/v{version:apiVersion}/fuel-cards")]
    [HttpGet("/api/fuel-cards")]
    [ProducesResponseType(typeof(PagedList<FuelCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<FuelCardDto>>> GetFuelCards(
        [FromQuery] GetFuelCardsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Registers a fuel card against the vehicle it was issued with.</summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-cards")]
    [HttpPost("/api/fuel-cards")]
    [Idempotent]
    [ProducesResponseType(typeof(FuelCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FuelCardDto>> AddFuelCard(
        AddFuelCardCommand command,
        CancellationToken cancellationToken)
    {
        var card = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetFuelCards), new { id = card.Id }, card);
    }

    /// <summary>Retires a fuel card. Its number can be reused once it is gone.</summary>
    [HttpDelete("/api/v{version:apiVersion}/fuel-cards/{id:guid}")]
    [HttpDelete("/api/fuel-cards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFuelCard(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteFuelCardCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Parses an uploaded monthly statement against a column mapping and
    /// reports what would happen, without writing anything.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-cards/import/preview")]
    [HttpPost("/api/fuel-cards/import/preview")]
    [RequestSizeLimit(FuelImportRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(FuelImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FuelImportPreviewDto>> PreviewFuelImport(
        [FromForm] FuelImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A statement file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();

        var result = await Mediator.Send(
            new PreviewFuelImportCommand
            {
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content,
                HasHeaderRow = request.HasHeaderRow,
                Mapping = request.ToMapping(),
            },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Commits the same statement and mapping the preview showed, creating a
    /// fuel <c>VehicleExpense</c> for every row that resolves cleanly.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-cards/import")]
    [HttpPost("/api/fuel-cards/import")]
    [RequestSizeLimit(FuelImportRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(FuelImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FuelImportResultDto>> ImportFuelTransactions(
        [FromForm] FuelImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A statement file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();

        var result = await Mediator.Send(
            new ImportFuelTransactionsCommand
            {
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content,
                HasHeaderRow = request.HasHeaderRow,
                Mapping = request.ToMapping(),
            },
            cancellationToken);

        return Ok(result);
    }

    // ---- vehicle rental/lease rates ---------------------------------------

    /// <summary>Lists rental/lease rates.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-rental-rates")]
    [HttpGet("/api/vehicle-rental-rates")]
    [ProducesResponseType(typeof(PagedList<VehicleRentalRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<VehicleRentalRateDto>>> GetVehicleRentalRates(
        [FromQuery] GetVehicleRentalRatesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total monthly amount of whatever the rental rates list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-rental-rates/summary")]
    [HttpGet("/api/vehicle-rental-rates/summary")]
    [ProducesResponseType(typeof(VehicleRentalRateSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VehicleRentalRateSummaryDto>> GetVehicleRentalRatesSummary(
        [FromQuery] GetVehicleRentalRatesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts a new rental/lease rate in force, closing off the one before it.</summary>
    [HttpPost("/api/v{version:apiVersion}/vehicle-rental-rates")]
    [HttpPost("/api/vehicle-rental-rates")]
    [Idempotent]
    [ProducesResponseType(typeof(VehicleRentalRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleRentalRateDto>> SetVehicleRentalRate(
        SetVehicleRentalRateCommand command,
        CancellationToken cancellationToken)
    {
        var rate = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetVehicleRentalRates), new { id = rate.Id }, rate);
    }

    /// <summary>Corrects a rental rate that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/vehicle-rental-rates/{id:guid}")]
    [HttpPut("/api/vehicle-rental-rates/{id:guid}")]
    [ProducesResponseType(typeof(VehicleRentalRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleRentalRateDto>> UpdateVehicleRentalRate(
        Guid id,
        UpdateVehicleRentalRateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a rental rate.</summary>
    [HttpDelete("/api/v{version:apiVersion}/vehicle-rental-rates/{id:guid}")]
    [HttpDelete("/api/vehicle-rental-rates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicleRentalRate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleRentalRateCommand(id), cancellationToken);
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

    // ---- tool rental/lease rates -------------------------------------------

    /// <summary>Lists rental/lease rates.</summary>
    [HttpGet("/api/v{version:apiVersion}/tool-rental-rates")]
    [HttpGet("/api/tool-rental-rates")]
    [ProducesResponseType(typeof(PagedList<ToolRentalRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<ToolRentalRateDto>>> GetToolRentalRates(
        [FromQuery] GetToolRentalRatesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total monthly amount of whatever the rental rates list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/tool-rental-rates/summary")]
    [HttpGet("/api/tool-rental-rates/summary")]
    [ProducesResponseType(typeof(ToolRentalRateSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ToolRentalRateSummaryDto>> GetToolRentalRatesSummary(
        [FromQuery] GetToolRentalRatesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts a new rental/lease rate in force, closing off the one before it.</summary>
    [HttpPost("/api/v{version:apiVersion}/tool-rental-rates")]
    [HttpPost("/api/tool-rental-rates")]
    [Idempotent]
    [ProducesResponseType(typeof(ToolRentalRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolRentalRateDto>> SetToolRentalRate(
        SetToolRentalRateCommand command,
        CancellationToken cancellationToken)
    {
        var rate = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetToolRentalRates), new { id = rate.Id }, rate);
    }

    /// <summary>Corrects a rental rate that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/tool-rental-rates/{id:guid}")]
    [HttpPut("/api/tool-rental-rates/{id:guid}")]
    [ProducesResponseType(typeof(ToolRentalRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolRentalRateDto>> UpdateToolRentalRate(
        Guid id,
        UpdateToolRentalRateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a rental rate.</summary>
    [HttpDelete("/api/v{version:apiVersion}/tool-rental-rates/{id:guid}")]
    [HttpDelete("/api/tool-rental-rates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteToolRentalRate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteToolRentalRateCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- vehicles rented out to other companies ----------------------------

    /// <summary>Lists loans of a vehicle out to another company — the revenue direction.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-rentals-out")]
    [HttpGet("/api/vehicle-rentals-out")]
    [ProducesResponseType(typeof(PagedList<VehicleRentalOutDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<VehicleRentalOutDto>>> GetVehicleRentalsOut(
        [FromQuery] GetVehicleRentalsOutQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count, how many are still out, and the total value of whatever the loans-out list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-rentals-out/summary")]
    [HttpGet("/api/vehicle-rentals-out/summary")]
    [ProducesResponseType(typeof(VehicleRentalOutSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VehicleRentalOutSummaryDto>> GetVehicleRentalsOutSummary(
        [FromQuery] GetVehicleRentalsOutSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records the vehicle going out to another company. Requires it to be available.</summary>
    [HttpPost("/api/v{version:apiVersion}/vehicle-rentals-out")]
    [HttpPost("/api/vehicle-rentals-out")]
    [Idempotent]
    [ProducesResponseType(typeof(VehicleRentalOutDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleRentalOutDto>> RecordVehicleRentalOut(
        RecordVehicleRentalOutCommand command,
        CancellationToken cancellationToken)
    {
        var rental = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetVehicleRentalsOut), new { id = rental.Id }, rental);
    }

    /// <summary>Closes an open loan — the vehicle came back — and frees the vehicle up again.</summary>
    [HttpPut("/api/v{version:apiVersion}/vehicle-rentals-out/{id:guid}/return")]
    [HttpPut("/api/vehicle-rentals-out/{id:guid}/return")]
    [ProducesResponseType(typeof(VehicleRentalOutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleRentalOutDto>> ReturnVehicleRentalOut(
        Guid id,
        ReturnVehicleRentalOutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Corrects a loan that was typed in wrong. Never touches whether it is returned.</summary>
    [HttpPut("/api/v{version:apiVersion}/vehicle-rentals-out/{id:guid}")]
    [HttpPut("/api/vehicle-rentals-out/{id:guid}")]
    [ProducesResponseType(typeof(VehicleRentalOutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleRentalOutDto>> UpdateVehicleRentalOut(
        Guid id,
        UpdateVehicleRentalOutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a loan-out record.</summary>
    [HttpDelete("/api/v{version:apiVersion}/vehicle-rentals-out/{id:guid}")]
    [HttpDelete("/api/vehicle-rentals-out/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicleRentalOut(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleRentalOutCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- tools rented out to other companies -------------------------------

    /// <summary>Lists loans of a tool out to another company — the revenue direction.</summary>
    [HttpGet("/api/v{version:apiVersion}/tool-rentals-out")]
    [HttpGet("/api/tool-rentals-out")]
    [ProducesResponseType(typeof(PagedList<ToolRentalOutDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<ToolRentalOutDto>>> GetToolRentalsOut(
        [FromQuery] GetToolRentalsOutQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count, how many are still out, and the total value of whatever the loans-out list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/tool-rentals-out/summary")]
    [HttpGet("/api/tool-rentals-out/summary")]
    [ProducesResponseType(typeof(ToolRentalOutSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ToolRentalOutSummaryDto>> GetToolRentalsOutSummary(
        [FromQuery] GetToolRentalsOutSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records the tool going out to another company. Requires it to be available.</summary>
    [HttpPost("/api/v{version:apiVersion}/tool-rentals-out")]
    [HttpPost("/api/tool-rentals-out")]
    [Idempotent]
    [ProducesResponseType(typeof(ToolRentalOutDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolRentalOutDto>> RecordToolRentalOut(
        RecordToolRentalOutCommand command,
        CancellationToken cancellationToken)
    {
        var rental = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetToolRentalsOut), new { id = rental.Id }, rental);
    }

    /// <summary>Closes an open loan — the tool came back — and frees the tool up again.</summary>
    [HttpPut("/api/v{version:apiVersion}/tool-rentals-out/{id:guid}/return")]
    [HttpPut("/api/tool-rentals-out/{id:guid}/return")]
    [ProducesResponseType(typeof(ToolRentalOutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolRentalOutDto>> ReturnToolRentalOut(
        Guid id,
        ReturnToolRentalOutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Corrects a loan that was typed in wrong. Never touches whether it is returned.</summary>
    [HttpPut("/api/v{version:apiVersion}/tool-rentals-out/{id:guid}")]
    [HttpPut("/api/tool-rentals-out/{id:guid}")]
    [ProducesResponseType(typeof(ToolRentalOutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolRentalOutDto>> UpdateToolRentalOut(
        Guid id,
        UpdateToolRentalOutCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a loan-out record.</summary>
    [HttpDelete("/api/v{version:apiVersion}/tool-rentals-out/{id:guid}")]
    [HttpDelete("/api/tool-rentals-out/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteToolRentalOut(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteToolRentalOutCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- general expenses ---------------------------------------------------

    /// <summary>Lists housing, bookkeeping, damage and every other cost with no ledger of its own.</summary>
    [HttpGet("/api/v{version:apiVersion}/general-expenses")]
    [HttpGet("/api/general-expenses")]
    [ProducesResponseType(typeof(PagedList<GeneralExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<GeneralExpenseDto>>> GetGeneralExpenses(
        [FromQuery] GetGeneralExpensesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total of whatever the general-expense list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/general-expenses/summary")]
    [HttpGet("/api/general-expenses/summary")]
    [ProducesResponseType(typeof(GeneralExpenseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GeneralExpenseSummaryDto>> GetGeneralExpensesSummary(
        [FromQuery] GetGeneralExpensesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records a cost that isn't a vehicle's, a tool's or a material's.</summary>
    [HttpPost("/api/v{version:apiVersion}/general-expenses")]
    [HttpPost("/api/general-expenses")]
    [Idempotent]
    [ProducesResponseType(typeof(GeneralExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneralExpenseDto>> RecordGeneralExpense(
        RecordGeneralExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetGeneralExpenses), new { id = expense.Id }, expense);
    }

    /// <summary>Corrects a general expense that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/general-expenses/{id:guid}")]
    [HttpPut("/api/general-expenses/{id:guid}")]
    [ProducesResponseType(typeof(GeneralExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneralExpenseDto>> UpdateGeneralExpense(
        Guid id,
        UpdateGeneralExpenseCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a recorded general expense.</summary>
    [HttpDelete("/api/v{version:apiVersion}/general-expenses/{id:guid}")]
    [HttpDelete("/api/general-expenses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGeneralExpense(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteGeneralExpenseCommand(id), cancellationToken);
        return NoContent();
    }

    // ---- accommodation rates ------------------------------------------------

    /// <summary>Lists rates for worker housing.</summary>
    [HttpGet("/api/v{version:apiVersion}/accommodation-rates")]
    [HttpGet("/api/accommodation-rates")]
    [ProducesResponseType(typeof(PagedList<AccommodationRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<AccommodationRateDto>>> GetAccommodationRates(
        [FromQuery] GetAccommodationRatesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>The count and total monthly amount of whatever the accommodation-rates list is currently filtered to.</summary>
    [HttpGet("/api/v{version:apiVersion}/accommodation-rates/summary")]
    [HttpGet("/api/accommodation-rates/summary")]
    [ProducesResponseType(typeof(AccommodationRateSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccommodationRateSummaryDto>> GetAccommodationRatesSummary(
        [FromQuery] GetAccommodationRatesSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts a new rate in force for an accommodation, closing off the one before it.</summary>
    [HttpPost("/api/v{version:apiVersion}/accommodation-rates")]
    [HttpPost("/api/accommodation-rates")]
    [Idempotent]
    [ProducesResponseType(typeof(AccommodationRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccommodationRateDto>> SetAccommodationRate(
        SetAccommodationRateCommand command,
        CancellationToken cancellationToken)
    {
        var rate = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetAccommodationRates), new { id = rate.Id }, rate);
    }

    /// <summary>Corrects an accommodation rate that was typed in wrong.</summary>
    [HttpPut("/api/v{version:apiVersion}/accommodation-rates/{id:guid}")]
    [HttpPut("/api/accommodation-rates/{id:guid}")]
    [ProducesResponseType(typeof(AccommodationRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccommodationRateDto>> UpdateAccommodationRate(
        Guid id,
        UpdateAccommodationRateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes an accommodation rate.</summary>
    [HttpDelete("/api/v{version:apiVersion}/accommodation-rates/{id:guid}")]
    [HttpDelete("/api/accommodation-rates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccommodationRate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAccommodationRateCommand(id), cancellationToken);
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

/// <summary>
/// The multipart form a fuel-statement upload arrives as — the file plus
/// which column is which, exactly as the preview step showed the user.
/// </summary>
/// <remarks>
/// Separate from the command for the same reason as
/// <see cref="UploadAttachmentRequest"/>: <see cref="IFormFile"/> is an
/// ASP.NET type the Application layer does not reference.
/// </remarks>
public class FuelImportRequest
{
    public IFormFile? File { get; set; }

    public bool HasHeaderRow { get; set; } = true;

    public int CardNumberColumn { get; set; }

    public int OccurredOnColumn { get; set; }

    public int AmountColumn { get; set; }

    public int LitresColumn { get; set; }

    public int? SupplierColumn { get; set; }

    public int? NoteColumn { get; set; }

    public int? OdometerColumn { get; set; }

    public int? FuelProductTypeColumn { get; set; }

    public FuelImportColumnMapping ToMapping() => new()
    {
        CardNumberColumn = CardNumberColumn,
        OccurredOnColumn = OccurredOnColumn,
        AmountColumn = AmountColumn,
        LitresColumn = LitresColumn,
        SupplierColumn = SupplierColumn,
        NoteColumn = NoteColumn,
        OdometerColumn = OdometerColumn,
        FuelProductTypeColumn = FuelProductTypeColumn,
    };
}
