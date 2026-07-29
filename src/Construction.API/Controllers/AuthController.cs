using Construction.Application.Features.Authentication.Commands.ChangePassword;
using Construction.Application.Features.Authentication.Commands.ForgotPassword;
using Construction.Application.Features.Authentication.Commands.Login;
using Construction.Application.Features.Authentication.Commands.Logout;
using Construction.Application.Features.Authentication.Commands.RefreshToken;
using Construction.Application.Features.Authentication.Commands.ResetPassword;
using Construction.Application.Features.Authentication.Models;
using Construction.Application.Features.Authentication.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class AuthController : ApiControllerBase
{
    /// <summary>Authenticates a user and returns an access/refresh token pair.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(command with { IpAddress = ClientIpAddress }, cancellationToken);
        return Ok(response);
    }

    /// <summary>Rotates the refresh token and returns a fresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(command with { IpAddress = ClientIpAddress }, cancellationToken);
        return Ok(response);
    }

    /// <summary>Revokes the presented refresh token, ending the session.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { IpAddress = ClientIpAddress }, cancellationToken);
        return NoContent();
    }

    /// <summary>Changes the current user's password and revokes all sessions.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Starts the password-reset flow. Always returns 202 regardless of whether
    /// the email exists, to prevent account enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return Accepted();
    }

    /// <summary>Completes the password-reset flow using the emailed token.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await Mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(user);
    }
}
