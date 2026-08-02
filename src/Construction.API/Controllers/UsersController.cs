using Construction.API.Authorization;
using Construction.API.Extensions;
using Construction.Application.Common.Models;
using Construction.Application.Features.Users.Commands.ActivateUser;
using Construction.Application.Features.Users.Commands.CreateUser;
using Construction.Application.Features.Users.Commands.DeactivateUser;
using Construction.Application.Features.Users.Commands.SetUserPassword;
using Construction.Application.Features.Users.Commands.UpdateUser;
using Construction.Application.Features.Users.Models;
using Construction.Application.Features.Users.Queries.GetUserById;
using Construction.Application.Features.Users.Queries.GetUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Controllers;

/// <summary>
/// Account administration: who can sign in, as what, and — the reason this
/// exists — how to take that away when someone leaves.
///
/// <para>
/// Every endpoint is Admin and above, and each handler additionally refuses to
/// act on an account senior to the caller or to grant a role the caller does
/// not hold. The policy decides who gets in; the handler decides who they may
/// touch once inside.
/// </para>
/// </summary>
public class UsersController : ApiControllerBase
{
    /// <summary>Lists accounts, including deactivated ones.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(PagedList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<UserDto>>> GetList(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one account.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetUserByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a sign-in account, optionally linked to an employee.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Updates an account's email, role or employee link.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Offboards an account: revokes every session, invalidates outstanding
    /// password-reset links, and removes the device registrations that would
    /// otherwise keep delivering push notifications.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeactivateUserCommand(id), cancellationToken);

        return NoContent();
    }

    /// <summary>Restores access to a deactivated account.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new ActivateUserCommand(id), cancellationToken));
    }

    /// <summary>
    /// Sets another account's password, for the many site workers who have no
    /// mailbox to receive a self-service reset. Rate limited like the other
    /// endpoints that accept a password.
    /// </summary>
    [HttpPost("{id:guid}/password")]
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetPassword(
        Guid id,
        SetUserPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { Id = id }, cancellationToken);

        return NoContent();
    }
}
