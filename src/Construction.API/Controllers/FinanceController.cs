using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The company's income, spending and profit.
/// </summary>
/// <remarks>
/// The route policy is only "internal staff": who may actually see money is
/// not a role but a grant on the account (<c>FinanceAccess</c>), which the
/// handlers check against the database on every call — see
/// <see cref="FinanceRules"/>.
/// </remarks>
[Authorize(Policy = Policies.AllEmployees)]
public class FinanceController : ApiControllerBase
{
    /// <summary>Income, spending and profit over a period, cut into days, weeks or months, with the period before it for comparison.</summary>
    [HttpGet("series")]
    [ProducesResponseType(typeof(FinanceSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FinanceSeriesDto>> GetSeries(
        [FromQuery] GetFinanceSeriesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Lists money received that belongs to no project — rentals and the like.</summary>
    [HttpGet("company-revenues")]
    [ProducesResponseType(typeof(PagedList<CompanyRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<CompanyRevenueDto>>> GetCompanyRevenues(
        [FromQuery] GetCompanyRevenuesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Records money received that belongs to no project.</summary>
    [HttpPost("company-revenues")]
    [Idempotent]
    [ProducesResponseType(typeof(CompanyRevenueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyRevenueDto>> RecordCompanyRevenue(
        RecordCompanyRevenueCommand command,
        CancellationToken cancellationToken)
    {
        var revenue = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetCompanyRevenues), new { id = revenue.Id }, revenue);
    }

    /// <summary>Corrects a revenue that was typed in wrong.</summary>
    [HttpPut("company-revenues/{id:guid}")]
    [ProducesResponseType(typeof(CompanyRevenueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyRevenueDto>> UpdateCompanyRevenue(
        Guid id,
        UpdateCompanyRevenueCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes a recorded revenue.</summary>
    [HttpDelete("company-revenues/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCompanyRevenue(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteCompanyRevenueCommand(id), cancellationToken);
        return NoContent();
    }
}
