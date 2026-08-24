using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Vehicles.Commands.AssignVehicle;
using Construction.Application.Features.Vehicles.Commands.AssignVehicleToProject;
using Construction.Application.Features.Vehicles.Commands.CreateVehicle;
using Construction.Application.Features.Vehicles.Commands.DeleteVehicle;
using Construction.Application.Features.Vehicles.Commands.SelfCheckOutVehicle;
using Construction.Application.Features.Vehicles.Commands.SelfReturnVehicle;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicle;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicleFromProject;
using Construction.Application.Features.Vehicles.Commands.UpdateVehicle;
using Construction.Application.Features.Vehicles.Models;
using Construction.Application.Features.Vehicles.Queries.GetVehicleById;
using Construction.Application.Features.Vehicles.Queries.GetVehicleByQrCode;
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

    /// <summary>
    /// Looks a vehicle up by its QR label — used by the mobile app after
    /// scanning. Available to every authenticated employee.
    /// </summary>
    [HttpGet("by-qr/{qrCode}")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDto>> GetByQrCode(
        string qrCode,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetVehicleByQrCodeQuery(qrCode), cancellationToken));
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
    [Idempotent]
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
    [Idempotent]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Unassign(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new UnassignVehicleCommand(id), cancellationToken));
    }

    /// <summary>Places (or moves) the vehicle onto a project. Any employee assignment is kept.</summary>
    [HttpPost("{id:guid}/assign-project/{projectId:guid}")]
    [Idempotent]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> AssignProject(
        Guid id,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(
            new AssignVehicleToProjectCommand(id, projectId), cancellationToken));
    }

    /// <summary>Removes the vehicle's project assignment.</summary>
    [HttpPost("{id:guid}/unassign-project")]
    [Idempotent]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> UnassignProject(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new UnassignVehicleFromProjectCommand(id), cancellationToken));
    }

    /// <summary>
    /// Checks the vehicle out to the calling employee. Used by the mobile app
    /// after scanning the vehicle's QR label. Available to every
    /// authenticated employee — self-checkout only, never on behalf of
    /// someone else.
    /// </summary>
    [HttpPost("{id:guid}/check-out-to-me")]
    [Idempotent]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> CheckOutToMe(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new SelfCheckOutVehicleCommand(id), cancellationToken));
    }

    /// <summary>
    /// Returns a vehicle that is checked out to the calling employee.
    /// Available to every authenticated employee — only releases the
    /// caller's own checkout, never someone else's.
    /// </summary>
    [HttpPost("{id:guid}/return")]
    [Idempotent]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Return(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new SelfReturnVehicleCommand(id), cancellationToken));
    }
}
