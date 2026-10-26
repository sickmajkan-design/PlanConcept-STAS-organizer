using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Features.VehicleFines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Fines a vehicle has collected. Entered, changed and removed by the office only (Admin and above); money and
/// names of drivers are not for everybody, so reading is the office's too.
/// </summary>
[Authorize(Policy = Policies.AdminAndAbove)]
public class VehicleFinesController : ApiControllerBase
{
    /// <summary>Lists one vehicle's fines, open ones first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleFineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VehicleFineDto>>> GetList(
        [FromQuery] Guid vehicleId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetVehicleFinesQuery(vehicleId), cancellationToken));
    }

    /// <summary>Enters a fine against a vehicle.</summary>
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(VehicleFineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleFineDto>> Add(
        AddVehicleFineCommand command,
        CancellationToken cancellationToken)
    {
        var fine = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { vehicleId = fine.VehicleId }, fine);
    }

    /// <summary>Changes a fine: its details, who bears it, or its state (paid, contested).</summary>
    [HttpPut("{id:guid}")]
    [Idempotent]
    [ProducesResponseType(typeof(VehicleFineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleFineDto>> Update(
        Guid id,
        UpdateVehicleFineCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a fine that should never have been entered.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleFineCommand(id), cancellationToken);

        return NoContent();
    }
}
