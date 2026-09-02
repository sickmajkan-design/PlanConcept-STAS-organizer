using Construction.API.Authorization;
using Construction.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;
using Construction.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;
using Construction.Application.Features.PublicHolidays.Commands.ImportPublicHolidays;
using Construction.Application.Features.PublicHolidays.Models;
using Construction.Application.Features.PublicHolidays.Queries.GetPublicHolidays;
using Construction.Application.Features.PublicHolidays.Queries.PreviewHolidaySync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The calendar a pay rate's holiday premium is priced against.
/// </summary>
/// <remarks>
/// Same route policy as <see cref="CostsController"/> — the finer split
/// between reading and managing the calendar lives in <c>CostRules</c>,
/// mirroring how pay rates themselves are gated.
/// </remarks>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class PublicHolidaysController : ApiControllerBase
{
    /// <summary>Lists the holiday calendar. Refused below Project Manager.</summary>
    [HttpGet("/api/v{version:apiVersion}/public-holidays")]
    [HttpGet("/api/public-holidays")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicHolidayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PublicHolidayDto>>> GetList(
        [FromQuery] GetPublicHolidaysQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Adds a date to the holiday calendar. Admin and above.</summary>
    [HttpPost("/api/v{version:apiVersion}/public-holidays")]
    [HttpPost("/api/public-holidays")]
    [ProducesResponseType(typeof(PublicHolidayDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublicHolidayDto>> Create(
        CreatePublicHolidayCommand command,
        CancellationToken cancellationToken)
    {
        var holiday = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = holiday.Id }, holiday);
    }

    /// <summary>
    /// Fetches a country's public holidays for one year from the internet,
    /// without writing anything — the review step before <see cref="Import"/>.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/public-holidays/sync-preview")]
    [HttpGet("/api/public-holidays/sync-preview")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicHolidayCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<PublicHolidayCandidateDto>>> PreviewSync(
        [FromQuery] PreviewHolidaySyncQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Adds the chosen holidays from a sync preview to the calendar. Admin and above.</summary>
    [HttpPost("/api/v{version:apiVersion}/public-holidays/import")]
    [HttpPost("/api/public-holidays/import")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicHolidayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PublicHolidayDto>>> Import(
        ImportPublicHolidaysCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Removes a date from the holiday calendar. Admin and above.</summary>
    [HttpDelete("/api/v{version:apiVersion}/public-holidays/{id:guid}")]
    [HttpDelete("/api/public-holidays/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeletePublicHolidayCommand(id), cancellationToken);
        return NoContent();
    }
}
