using Construction.API.Authorization;
using Construction.Application.Features.Exports.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Spreadsheet exports of the things people take out of the system: the
/// hours, the costs, the stock.
/// </summary>
/// <remarks>
/// Every action returns a file, so the usual JSON conventions do not apply —
/// but the authorisation does. Each handler applies the same rule its screen
/// does, and the cost exports go through the report queries themselves rather
/// than repeating their arithmetic, so an export can never show a figure the
/// screen would have withheld.
/// </remarks>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class ExportsController : ApiControllerBase
{
    /// <summary>The hours, row by row. Refused to workers.</summary>
    [HttpGet("/api/v{version:apiVersion}/exports/time-entries")]
    [HttpGet("/api/exports/time-entries")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportTimeEntries(
        [FromQuery] ExportTimeEntriesQuery query,
        CancellationToken cancellationToken)
    {
        return Download(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// What each site cost. Below Project Manager the labour columns are
    /// absent, exactly as they are on screen.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/exports/project-costs")]
    [HttpGet("/api/exports/project-costs")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportProjectCosts(
        [FromQuery] ExportProjectCostsQuery query,
        CancellationToken cancellationToken)
    {
        return Download(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>What the fleet cost, and what it drank.</summary>
    [HttpGet("/api/v{version:apiVersion}/exports/vehicle-costs")]
    [HttpGet("/api/exports/vehicle-costs")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportVehicleCosts(
        [FromQuery] ExportVehicleCostsQuery query,
        CancellationToken cancellationToken)
    {
        return Download(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Deliveries, issues and corrections over a period.</summary>
    [HttpGet("/api/v{version:apiVersion}/exports/material-movements")]
    [HttpGet("/api/exports/material-movements")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportMaterialMovements(
        [FromQuery] ExportMaterialMovementsQuery query,
        CancellationToken cancellationToken)
    {
        return Download(await Mediator.Send(query, cancellationToken));
    }

    /// <remarks>
    /// The file name is built by the handler and is already ASCII, so it needs
    /// no RFC 5987 encoding — a name with Serbian diacritics would otherwise
    /// arrive mangled or quoted depending on the browser.
    /// </remarks>
    private FileContentResult Download(ExportFile file) =>
        File(file.Content, file.ContentType, file.FileName);
}
