using Construction.Application.Features.Authentication.Commands.ChangePassword;
using Construction.Application.Features.Authentication.Commands.ForgotPassword;
using Construction.Application.Features.Authentication.Commands.Login;
using Construction.Application.Features.Authentication.Commands.Logout;
using Construction.Application.Features.Authentication.Commands.RefreshToken;
using Construction.Application.Features.Authentication.Commands.ResetPassword;
using Construction.Application.Features.Authentication.Models;
using Construction.Application.Features.Authentication.Queries.GetCurrentUser;
using Construction.API.Authentication;
using Construction.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Controllers;

public class AuthController : ApiControllerBase
{
    private RefreshTokenCookie Cookie =>
        HttpContext.RequestServices.GetRequiredService<RefreshTokenCookie>();

    /// <summary>Authenticates a user and returns an access/refresh token pair.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(command with { IpAddress = ClientIpAddress }, cancellationToken);

        return Ok(IssueCookieIfAsked(response));
    }

    /// <summary>Rotates the refresh token and returns a fresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        TokenRequest? request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new RefreshTokenCommand
            {
                // A browser sends nothing in the body; the token it holds is
                // in a cookie it cannot read.
                RefreshToken = Cookie.Read(HttpContext, request?.RefreshToken)!,
                IpAddress = ClientIpAddress,
            },
            cancellationToken);

        return Ok(IssueCookieIfAsked(response));
    }

    /// <summary>Revokes the presented refresh token, ending the session.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        TokenRequest? request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new LogoutCommand
            {
                RefreshToken = Cookie.Read(HttpContext, request?.RefreshToken)!,
                IpAddress = ClientIpAddress,
            },
            cancellationToken);

        // Unconditionally, not only in cookie mode: signing out has to leave
        // nothing behind, and a client that switched modes mid-session would
        // otherwise keep a cookie nobody clears.
        Cookie.Clear(HttpContext);

        return NoContent();
    }

    /// <summary>Changes the current user's password and revokes all sessions.</summary>
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);

        // Every session is revoked by the command, so the cookie now carries a
        // token the API will refuse. Leaving it would mean the browser keeps
        // presenting a dead credential until it expires.
        Cookie.Clear(HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Starts the password-reset flow. Always returns 202 regardless of whether
    /// the email exists, to prevent account enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
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
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
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

    /// <summary>
    /// Moves the refresh token into a cookie when the caller asked for one.
    /// </summary>
    private AuthResponse IssueCookieIfAsked(AuthResponse response) =>
        RefreshTokenCookie.WantsCookie(HttpContext)
            ? Cookie.Issue(HttpContext, response)
            : response;

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
