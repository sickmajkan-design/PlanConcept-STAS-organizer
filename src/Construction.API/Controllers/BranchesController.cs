using Construction.API.Authorization;
using Construction.Application.Features.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The operator's own business units (poslovne jedinice). Anyone on the office side reads them
/// to filter by; only management changes them (see <c>BranchRules</c>).
/// </summary>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class BranchesController : ApiControllerBase
{
    /// <summary>Lists the business units, active first.</summary>
    [HttpGet("/api/v{version:apiVersion}/branches")]
    [HttpGet("/api/branches")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetBranchesQuery(), cancellationToken));
    }

    /// <summary>Adds a business unit. Admin and above.</summary>
    [HttpPost("/api/v{version:apiVersion}/branches")]
    [HttpPost("/api/branches")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> Create(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = branch.Id }, branch);
    }

    /// <summary>Renames, recolours or switches a business unit off or on. Admin and above.</summary>
    [HttpPut("/api/v{version:apiVersion}/branches/{id:guid}")]
    [HttpPut("/api/branches/{id:guid}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> Update(Guid id, UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Sets the Main projects of a business unit (their sub-projects follow). Projects that were in
    /// it and are not listed are released. Admin and above.
    /// </summary>
    [HttpPut("/api/v{version:apiVersion}/branches/{id:guid}/projects")]
    [HttpPut("/api/branches/{id:guid}/projects")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchDto>> SetProjects(
        Guid id,
        SetBranchProjectsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Moves several employees into a business unit from a date. Everyone else keeps their unit.
    /// All or nothing. Admin and above.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/branches/{id:guid}/employees")]
    [HttpPost("/api/branches/{id:guid}/employees")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> AssignEmployees(
        Guid id,
        AssignEmployeesToBranchCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Deletes a business unit with no projects. Admin and above.</summary>
    [HttpDelete("/api/v{version:apiVersion}/branches/{id:guid}")]
    [HttpDelete("/api/branches/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteBranchCommand(id), cancellationToken);
        return NoContent();
    }
}
