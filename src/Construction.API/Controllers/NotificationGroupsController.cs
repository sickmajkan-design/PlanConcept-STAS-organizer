using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;
using Construction.Application.Features.NotificationGroups.Commands.DeleteNotificationGroup;
using Construction.Application.Features.NotificationGroups.Commands.UpdateNotificationGroup;
using Construction.Application.Features.NotificationGroups.Models;
using Construction.Application.Features.NotificationGroups.Queries.GetNotificationGroupById;
using Construction.Application.Features.NotificationGroups.Queries.GetNotificationGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Named, admin-defined lists of employees used only to narrow who an
/// announcement reaches. Admin and above throughout, same as sending one.
/// </summary>
[Authorize(Policy = Policies.AdminAndAbove)]
public class NotificationGroupsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<NotificationGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<NotificationGroupDto>>> GetList(
        [FromQuery] GetNotificationGroupsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationGroupDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationGroupDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetNotificationGroupByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(NotificationGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NotificationGroupDto>> Create(
        CreateNotificationGroupCommand command,
        CancellationToken cancellationToken)
    {
        var group = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NotificationGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NotificationGroupDto>> Update(
        Guid id,
        UpdateNotificationGroupCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteNotificationGroupCommand(id), cancellationToken);
        return NoContent();
    }
}
