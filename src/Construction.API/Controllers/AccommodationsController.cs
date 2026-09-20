using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Accommodations.Commands.CreateAccommodation;
using Construction.Application.Features.Accommodations.Costs;
using Construction.Application.Features.Accommodations.Stays;
using Construction.Application.Features.Accommodations.Commands.DeleteAccommodation;
using Construction.Application.Features.Accommodations.Commands.UpdateAccommodation;
using Construction.Application.Features.Accommodations.Models;
using Construction.Application.Features.Accommodations.Queries.GetAccommodationById;
using Construction.Application.Features.Accommodations.Queries.GetAccommodations;
using Construction.Application.Features.Accommodations.Queries.GetMyHousing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class AccommodationsController : ApiControllerBase
{
    /// <summary>Where the caller lives now (or will next): the worker-facing view, without any cost figures.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(MyHousingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<MyHousingDto?>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetMyHousingQuery(), cancellationToken));
    }

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
    /// <summary>Who lives (or lived) where. Filter by accommodation or by person.</summary>
    [HttpGet("stays")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<AccommodationStayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<AccommodationStayDto>>> GetStays(
        [FromQuery] GetAccommodationStaysQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Puts a person into this accommodation.</summary>
    [HttpPost("{id:guid}/stays")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(AccommodationStayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccommodationStayDto>> AddStay(
        Guid id,
        AddAccommodationStayCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { AccommodationId = id }, cancellationToken));
    }

    /// <summary>Corrects a stay: moves its dates, ends it, or changes the project it is charged to.</summary>
    [HttpPut("stays/{stayId:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(AccommodationStayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccommodationStayDto>> UpdateStay(
        Guid stayId,
        UpdateAccommodationStayCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = stayId }, cancellationToken));
    }

    /// <summary>Removes a stay that should never have been recorded.</summary>
    [HttpDelete("stays/{stayId:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStay(Guid stayId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAccommodationStayCommand(stayId), cancellationToken);
        return NoContent();
    }

    /// <summary>What this accommodation cost over a period, and who it was for.</summary>
    [HttpGet("{id:guid}/costs")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(AccommodationCostSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccommodationCostSummaryDto>> GetCosts(
        Guid id,
        [FromQuery] GetAccommodationCostsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query with { AccommodationId = id }, cancellationToken));
    }

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
