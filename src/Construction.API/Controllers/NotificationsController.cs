using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using Construction.Application.Features.Notifications.Commands.MarkNotificationRead;
using Construction.Application.Features.Notifications.Commands.RegisterDeviceToken;
using Construction.Application.Features.Notifications.Commands.SendAnnouncement;
using Construction.Application.Features.Notifications.Commands.UnregisterDeviceToken;
using Construction.Application.Features.Notifications.Models;
using Construction.Application.Features.Notifications.Queries.GetMyNotifications;
using Construction.Application.Features.Notifications.Queries.GetUnreadCount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class NotificationsController : ApiControllerBase
{
    /// <summary>The current user's notification inbox, newest first.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(PagedList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<NotificationDto>>> GetMine(
        [FromQuery] GetMyNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Unread notification count for the app badge.</summary>
    [HttpGet("unread-count")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetUnreadCountQuery(), cancellationToken));
    }

    /// <summary>Marks one notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Marks all notifications as read; returns how many changed.</summary>
    [HttpPost("read-all")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> MarkAllRead(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new MarkAllNotificationsReadCommand(), cancellationToken));
    }

    /// <summary>Registers (or refreshes) an FCM device token for the current user.</summary>
    [HttpPost("device-tokens")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDeviceToken(
        RegisterDeviceTokenCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Removes an FCM device token (called on logout). Idempotent.</summary>
    [HttpPost("device-tokens/unregister")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnregisterDeviceToken(
        UnregisterDeviceTokenCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sends a general announcement to all active users, optionally narrowed
    /// to one role and/or one project's crew. Returns the recipient count.
    /// </summary>
    [HttpPost("announce")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Announce(
        SendAnnouncementCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }
}
