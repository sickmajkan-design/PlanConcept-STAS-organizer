using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Models;
using Construction.Application.Features.Costs.Queries.GetMyFuel;
using Construction.Application.Features.Vehicles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Fuel as the person at the pump meets it: the vehicle in their hands, the fill-up they record, and
/// the fill-ups they have already recorded.
/// </summary>
/// <remarks>
/// Open to every employee role. The rest of the cost module is Foreman and above; this is the one
/// slice a driver needs, and the handler narrows it further: someone below Foreman may record fuel
/// only for a vehicle assigned to them. Kept apart from <see cref="CostsController"/> so that
/// controller's blanket route policy stays what its remarks say it is.
/// </remarks>
[Authorize(Policy = Policies.AllEmployees)]
public class VehicleFuelController : ApiControllerBase
{
    /// <summary>The vehicles signed out to the caller.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-fuel/vehicles")]
    [HttpGet("/api/vehicle-fuel/vehicles")]
    [ProducesResponseType(typeof(List<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VehicleDto>>> GetMyVehicles(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetMyVehiclesQuery(), cancellationToken));
    }

    /// <summary>The fill-ups the caller has recorded.</summary>
    [HttpGet("/api/v{version:apiVersion}/vehicle-fuel")]
    [HttpGet("/api/vehicle-fuel")]
    [ProducesResponseType(typeof(PagedList<VehicleExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<VehicleExpenseDto>>> GetMyFuel(
        [FromQuery] GetMyFuelExpensesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records a fill-up on a vehicle the caller may record fuel for.</summary>
    [HttpPost("/api/v{version:apiVersion}/vehicle-fuel")]
    [HttpPost("/api/vehicle-fuel")]
    [Idempotent]
    [ProducesResponseType(typeof(VehicleExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleExpenseDto>> Record(
        RecordVehicleExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetMyFuel), new { id = expense.Id }, expense);
    }
}
