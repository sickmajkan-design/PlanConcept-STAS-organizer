using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Employees.Commands.CreateEmployee;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Employees.Commands.RemoveEmployeeFromProject;
using Construction.Application.Features.Employees.Commands.UpdateEmployee;
using Construction.Application.Features.Employees.Models;
using Construction.Application.Features.Employees.Queries.GetEmployeeById;
using Construction.Application.Features.Employees.Queries.GetEmployees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Construction.API.Controllers;

public class EmployeesController : ApiControllerBase
{
    /// <summary>Lists employees with pagination, search, filtering and sorting.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<EmployeeDto>>> GetList(
        [FromQuery] GetEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one employee including project assignments.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetEmployeeByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a new employee.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeDto>> Create(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var employee = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    /// <summary>Updates an existing employee.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeDto>> Update(
        Guid id,
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes an employee and deactivates any linked user account.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteEmployeeCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Assigns the employee to a project. Dates are optional in the body —
    /// omitted, a posting starts today and runs open-ended.
    /// </summary>
    [HttpPost("{id:guid}/projects/{projectId:guid}")]
    [Idempotent]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignToProject(
        Guid id,
        Guid projectId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        AssignEmployeeToProjectCommand? command,
        CancellationToken cancellationToken)
    {
        // The dates are optional, and so is the body that would have carried
        // them. Without this, MVC answers 415 before the handler is reached
        // for any POST with no body at all — which is what every caller sent
        // before postings had dates, and what the employee detail page still
        // sends when somebody is put on a site with no end in mind.
        command ??= new AssignEmployeeToProjectCommand(id, projectId);

        await Mediator.Send(command with { EmployeeId = id, ProjectId = projectId }, cancellationToken);
        return NoContent();
    }

    /// <summary>Removes the employee from a project.</summary>
    [HttpDelete("{id:guid}/projects/{projectId:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromProject(
        Guid id,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RemoveEmployeeFromProjectCommand(id, projectId), cancellationToken);
        return NoContent();
    }
}
