using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Projects.Commands.CreateProject;
using Construction.Application.Features.Projects.Commands.DeleteProject;
using Construction.Application.Features.Projects.Commands.UpdateProject;
using Construction.Application.Features.Projects.Models;
using Construction.Application.Features.Projects.Queries.GetProjectById;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class ProjectsController : ApiControllerBase
{
    /// <summary>Lists projects with pagination, search, filtering and sorting.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<ProjectDto>>> GetList(
        [FromQuery] GetProjectsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one project including its employee roster.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(ProjectDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetProjectByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a new project.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var project = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    /// <summary>Updates an existing project.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Update(
        Guid id,
        UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes a project and releases its tool assignments.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }
}
