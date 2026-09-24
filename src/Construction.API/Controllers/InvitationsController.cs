using Construction.API.Authorization;
using Construction.API.Extensions;
using Construction.Application.Features.Invitations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Controllers;

/// <summary>
/// One-time sign-up links, so an employee creates their own account instead of
/// an administrator inventing and passing on a password.
/// </summary>
public class InvitationsController : ApiControllerBase
{
    /// <summary>Creates a link for an employee who has no account yet. Shown once.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(EmployeeInvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeInvitationDto>> Create(
        CreateEmployeeInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var invitation = await Mediator.Send(command, cancellationToken);

        // Never cached: the response carries a live token.
        Response.Headers.CacheControl = "no-store";

        return StatusCode(StatusCodes.Status201Created, invitation);
    }

    /// <summary>Checks a link without using it.</summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
    [ProducesResponseType(typeof(InvitationPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitationPreviewDto>> Get(string token, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        return Ok(await Mediator.Send(new GetInvitationQuery(token), cancellationToken));
    }

    /// <summary>Creates the account the link was for.</summary>
    [HttpPost("{token}/accept")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.CredentialsPolicy)]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AcceptInvitationResponse>> Accept(
        string token,
        [FromBody] AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var email = await Mediator.Send(
            new AcceptInvitationCommand { Token = token, Email = request.Email, Password = request.Password },
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new AcceptInvitationResponse(email));
    }
}

/// <summary>The body of an accept call; the token travels in the route.</summary>
public class AcceptInvitationRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public record AcceptInvitationResponse(string Email);
