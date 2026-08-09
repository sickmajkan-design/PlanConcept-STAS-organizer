using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Tools.Commands.AssignTool;
using Construction.Application.Features.Tools.Commands.CreateTool;
using Construction.Application.Features.Tools.Commands.DeleteTool;
using Construction.Application.Features.Tools.Commands.UnassignTool;
using Construction.Application.Features.Tools.Commands.UpdateTool;
using Construction.Application.Features.Tools.Models;
using Construction.Application.Features.Tools.Queries.GetToolById;
using Construction.Application.Features.Tools.Queries.GetToolByQrCode;
using Construction.Application.Features.Tools.Queries.GetTools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class ToolsController : ApiControllerBase
{
    /// <summary>Lists tools with pagination, search, filtering and sorting.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<ToolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<ToolDto>>> GetList(
        [FromQuery] GetToolsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one tool.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetToolByIdQuery(id), cancellationToken));
    }

    /// <summary>
    /// Looks a tool up by its QR label — used by the mobile app after scanning.
    /// Available to every authenticated employee.
    /// </summary>
    [HttpGet("by-qr/{qrCode}")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolDto>> GetByQrCode(
        string qrCode,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetToolByQrCodeQuery(qrCode), cancellationToken));
    }

    /// <summary>Creates a new tool.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolDto>> Create(
        CreateToolCommand command,
        CancellationToken cancellationToken)
    {
        var tool = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tool.Id }, tool);
    }

    /// <summary>Updates an existing tool.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolDto>> Update(
        Guid id,
        UpdateToolCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes a tool, clearing its assignments.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteToolCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Hands the tool to an employee (reassignment allowed).</summary>
    [HttpPost("{id:guid}/assign-employee/{employeeId:guid}")]
    [Idempotent]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolDto>> AssignEmployee(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new AssignToolToEmployeeCommand(id, employeeId), cancellationToken));
    }

    /// <summary>Releases the tool from its employee.</summary>
    [HttpPost("{id:guid}/unassign-employee")]
    [Idempotent]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolDto>> UnassignEmployee(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(
            new UnassignToolCommand(id, ToolAssignmentTarget.Employee), cancellationToken));
    }

    /// <summary>Places the tool on a project (reassignment allowed).</summary>
    [HttpPost("{id:guid}/assign-project/{projectId:guid}")]
    [Idempotent]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolDto>> AssignProject(
        Guid id,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new AssignToolToProjectCommand(id, projectId), cancellationToken));
    }

    /// <summary>Releases the tool from its project.</summary>
    [HttpPost("{id:guid}/unassign-project")]
    [Idempotent]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolDto>> UnassignProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(
            new UnassignToolCommand(id, ToolAssignmentTarget.Project), cancellationToken));
    }
}
