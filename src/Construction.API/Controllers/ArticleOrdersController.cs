using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.ArticleOrders.Commands.CreateArticleOrder;
using Construction.Application.Features.ArticleOrders.Commands.SetArticleOrderStatus;
using Construction.Application.Features.ArticleOrders.Models;
using Construction.Application.Features.ArticleOrders.Queries.GetArticleOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Requests for articles a person needs for the job: asked for, ordered, on the
/// way, delivered.
/// </summary>
/// <remarks>
/// Open to every signed-in employee at the route, because anyone may ask. What
/// each role may see and which step it may take lives in
/// <see cref="Application.Features.ArticleOrders.ArticleOrderRules"/>.
/// </remarks>
public class ArticleOrdersController : ApiControllerBase
{
    /// <summary>Lists requests. The office sees all; everyone else what they asked for.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(PagedList<ArticleOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<ArticleOrderDto>>> GetList(
        [FromQuery] GetArticleOrdersQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Asks for articles.</summary>
    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(ArticleOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleOrderDto>> Create(
        CreateArticleOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = order.Id }, order);
    }

    /// <summary>Moves a request one step on.</summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(ArticleOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArticleOrderDto>> SetStatus(
        Guid id,
        SetArticleOrderStatusCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }
}
