using Construction.API.Authorization;
using Construction.Application.Features.Overview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The company's money at a glance: profit by month from hours to profit, the next days of cash, and an
/// estimate for next month.
/// </summary>
/// <remarks>
/// Like <see cref="FinanceController"/>, the route policy is only "internal staff"; the handlers decide from
/// the finance grant on the account whether amounts or only statistics are shown.
/// </remarks>
[Authorize(Policy = Policies.AllEmployees)]
public class OverviewController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(OverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OverviewDto>> Get(
        [FromQuery] GetOverviewQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpGet("cashflow")]
    [ProducesResponseType(typeof(CashflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CashflowDto>> GetCashflow(
        [FromQuery] GetCashflowQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpGet("attention")]
    [ProducesResponseType(typeof(IReadOnlyList<OverviewAttentionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OverviewAttentionDto>>> GetAttention(
        [FromQuery] GetOverviewAttentionQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpGet("forecast")]
    [ProducesResponseType(typeof(ForecastDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ForecastDto>> GetForecast(
        [FromQuery] GetForecastQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}
