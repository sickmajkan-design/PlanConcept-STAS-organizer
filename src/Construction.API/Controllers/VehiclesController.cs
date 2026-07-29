using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Vehicles.Commands.AssignVehicle;
using Construction.Application.Features.Vehicles.Commands.CreateVehicle;
using Construction.Application.Features.Vehicles.Commands.DeleteVehicle;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicle;
using Construction.Application.Features.Vehicles.Commands.UpdateVehicle;
using Construction.Application.Features.Vehicles.Models;
using Construction.Application.Features.Vehicles.Queries.GetVehicleById;
using Construction.Application.Features.Vehicles.Queries.GetVehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class VehiclesController : ApiControllerBase
{
    /// <summary>Lists vehicles with pagination, search, filtering and sorting.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<VehicleDto>>> GetList(
        [FromQuery] GetVehiclesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one vehicle.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetVehicleByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a new vehicle.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Create(
        CreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var vehicle = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    /// <summary>Updates an existing vehicle.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Update(
        Guid id,
        UpdateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes a vehicle, clearing any employee assignment.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Assigns (or reassigns) the vehicle to an employee.</summary>
    [HttpPost("{id:guid}/assign/{employeeId:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Assign(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new AssignVehicleCommand(id, employeeId), cancellationToken));
    }

    /// <summary>Removes the vehicle's employee assignment.</summary>
    [HttpPost("{id:guid}/unassign")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Unassign(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new UnassignVehicleCommand(id), cancellationToken));
    }
}
