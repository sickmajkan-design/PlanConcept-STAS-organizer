using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Features.VehicleTolls.Commands.AddVehicleToll;
using Construction.Application.Features.VehicleTolls.Commands.DeleteVehicleToll;
using Construction.Application.Features.VehicleTolls.Commands.MarkVehicleTollPaid;
using Construction.Application.Features.VehicleTolls.Commands.UpdateVehicleToll;
using Construction.Application.Features.VehicleTolls.Models;
using Construction.Application.Features.VehicleTolls.Queries.GetVehicleTolls;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Vignettes, tunnel tolls and road-passage charges carried by a vehicle.
/// Read is open to every employee role — a driver needs to see what is (or
/// is not) paid before setting off; adding, editing and deleting are
/// Admin/SuperAdmin only, the same split as <c>AttachmentsController</c>.
/// </summary>
public class VehicleTollsController : ApiControllerBase
{
    /// <summary>Lists one vehicle's tolls, expired/expiring-soon first.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleTollDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<VehicleTollDto>>> GetList(
        [FromQuery] Guid vehicleId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetVehicleTollsQuery(vehicleId), cancellationToken));
    }

    /// <summary>Adds a toll to a vehicle, optionally already marked paid.</summary>
    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(VehicleTollDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleTollDto>> Add(
        AddVehicleTollCommand command,
        CancellationToken cancellationToken)
    {
        var toll = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { vehicleId = toll.VehicleId }, toll);
    }

    /// <summary>Marks a toll paid — the initial payment or a renewal. Always appends to the payment history.</summary>
    [HttpPut("{id:guid}/pay")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(VehicleTollDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleTollDto>> Pay(
        Guid id,
        MarkVehicleTollPaidCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { VehicleTollId = id }, cancellationToken));
    }

    /// <summary>Edits a toll's type, country and route segment. Never touches payment state.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(VehicleTollDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleTollDto>> Update(
        Guid id,
        UpdateVehicleTollCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { VehicleTollId = id }, cancellationToken));
    }

    /// <summary>Removes a toll entirely.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleTollCommand(id), cancellationToken);
        return NoContent();
    }
}
