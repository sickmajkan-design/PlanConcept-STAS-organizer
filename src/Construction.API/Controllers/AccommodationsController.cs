using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Accommodations.Commands.CreateAccommodation;
using Construction.Application.Features.Accommodations.Commands.DeleteAccommodation;
using Construction.Application.Features.Accommodations.Commands.UpdateAccommodation;
using Construction.Application.Features.Accommodations.Models;
using Construction.Application.Features.Accommodations.Queries.GetAccommodationById;
using Construction.Application.Features.Accommodations.Queries.GetAccommodations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class AccommodationsController : ApiControllerBase
{
    /// <summary>Lists accommodations with pagination, search and sorting.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<AccommodationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<AccommodationDto>>> GetList(
        [FromQuery] GetAccommodationsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one accommodation.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(AccommodationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccommodationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetAccommodationByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a new accommodation.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(AccommodationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccommodationDto>> Create(
        CreateAccommodationCommand command,
        CancellationToken cancellationToken)
    {
        var accommodation = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = accommodation.Id }, accommodation);
    }

    /// <summary>Updates an existing accommodation.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(AccommodationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccommodationDto>> Update(
        Guid id,
        UpdateAccommodationCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes an accommodation.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAccommodationCommand(id), cancellationToken);
        return NoContent();
    }
}
