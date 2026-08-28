using Construction.API.Authorization;
using Construction.Application.Features.Bulletin.Commands.CreateBulletinPost;
using Construction.Application.Features.Bulletin.Commands.DeleteBulletinPost;
using Construction.Application.Features.Bulletin.Commands.MarkBulletinViewed;
using Construction.Application.Features.Bulletin.Models;
using Construction.Application.Features.Bulletin.Queries.GetBulletinPosts;
using Construction.Application.Features.Bulletin.Queries.GetBulletinViewers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>The company bulletin board: notices posted until an admin removes them.</summary>
public class BulletinController : ApiControllerBase
{
    /// <summary>Every notice currently posted, newest first. Open to everyone signed in.</summary>
    [HttpGet("/api/v{version:apiVersion}/bulletin")]
    [HttpGet("/api/bulletin")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(IReadOnlyList<BulletinPostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BulletinPostDto>>> GetList(
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetBulletinPostsQuery(), cancellationToken));
    }

    /// <summary>Pins a new notice to the board. Admin and above.</summary>
    [HttpPost("/api/v{version:apiVersion}/bulletin")]
    [HttpPost("/api/bulletin")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(BulletinPostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulletinPostDto>> Create(
        CreateBulletinPostCommand command,
        CancellationToken cancellationToken)
    {
        var post = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = post.Id }, post);
    }

    /// <summary>Takes a notice down. Admin and above.</summary>
    [HttpDelete("/api/v{version:apiVersion}/bulletin/{id:guid}")]
    [HttpDelete("/api/bulletin/{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteBulletinPostCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Records that the caller has seen this notice. Open to everyone signed in.</summary>
    [HttpPost("/api/v{version:apiVersion}/bulletin/{id:guid}/view")]
    [HttpPost("/api/bulletin/{id:guid}/view")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkViewed(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new MarkBulletinViewedCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Who has seen this notice, and when. Admin and above.</summary>
    [HttpGet("/api/v{version:apiVersion}/bulletin/{id:guid}/viewers")]
    [HttpGet("/api/bulletin/{id:guid}/viewers")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(IReadOnlyList<BulletinViewerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BulletinViewerDto>>> GetViewers(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetBulletinViewersQuery(id), cancellationToken));
    }
}
