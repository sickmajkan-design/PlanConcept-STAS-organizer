using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.WorkItems.Commands.ChangeWorkItemStatus;
using Construction.Application.Features.WorkItems.Commands.CreateWorkItem;
using Construction.Application.Features.WorkItems.Commands.DeleteWorkItem;
using Construction.Application.Features.WorkItems.Commands.UpdateWorkItem;
using Construction.Application.Features.WorkItems.Models;
using Construction.Application.Features.WorkItems.Queries.GetWorkItemById;
using Construction.Application.Features.WorkItems.Queries.GetWorkItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Tasks and defects.
/// </summary>
/// <remarks>
/// Open to every signed-in employee at the route, because a Worker has to
/// reach their own list and report a defect from site. What each role may
/// actually see and change lives in <see cref="WorkItemRules"/>.
/// </remarks>
public class WorkItemsController : ApiControllerBase
{
    /// <summary>Lists work. Workers see only what is assigned to them.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(PagedList<WorkItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<WorkItemDto>>> GetList(
        [FromQuery] GetWorkItemsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one item. Workers may only read their own.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkItemDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetWorkItemByIdQuery(id), cancellationToken));
    }

    /// <summary>
    /// Raises a task, or reports a defect. A Worker may do the latter only.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkItemDto>> Create(
        CreateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        var item = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    /// <summary>Edits an item's details. Status moves through its own endpoint.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkItemDto>> Update(
        Guid id,
        UpdateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Moves an item to another state — the action a worker performs from site.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkItemDto>> ChangeStatus(
        Guid id,
        ChangeWorkItemStatusCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes an item. Cancel it instead unless it was a mistake.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteWorkItemCommand(id), cancellationToken);
        return NoContent();
    }
}
