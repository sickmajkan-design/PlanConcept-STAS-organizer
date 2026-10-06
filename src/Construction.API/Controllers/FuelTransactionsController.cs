using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.FuelCards.Import;
using Construction.Application.Features.FuelTransactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The DKV statement: upload it, see which rows line up with what drivers
/// recorded, and settle the rest. Office work only. The finer check lives in
/// <c>CostRules.CanImportFuelStatements</c>; the policy here keeps everyone else out
/// before a handler runs.
/// </summary>
[Authorize(Policy = Policies.AdminAndAbove)]
public class FuelTransactionsController : ApiControllerBase
{
    /// <summary>Lists statement rows, newest first, optionally only those in given states.</summary>
    [HttpGet("/api/v{version:apiVersion}/fuel-transactions")]
    [HttpGet("/api/fuel-transactions")]
    [ProducesResponseType(typeof(PagedList<FuelTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<FuelTransactionDto>>> GetList(
        [FromQuery] GetFuelTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>How many statement rows sit in each state.</summary>
    [HttpGet("/api/v{version:apiVersion}/fuel-transactions/counts")]
    [HttpGet("/api/fuel-transactions/counts")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Dictionary<string, int>>> GetCounts(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetFuelTransactionCountsQuery(), cancellationToken));
    }

    /// <summary>Earlier uploads: who, when, and what each one did.</summary>
    [HttpGet("/api/v{version:apiVersion}/fuel-transactions/batches")]
    [HttpGet("/api/fuel-transactions/batches")]
    [ProducesResponseType(typeof(PagedList<FuelImportBatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<FuelImportBatchDto>>> GetBatches(
        [FromQuery] GetFuelImportBatchesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Reads an uploaded DKV statement and reports what importing it would do, without writing anything.</summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-transactions/import/preview")]
    [HttpPost("/api/fuel-transactions/import/preview")]
    [RequestSizeLimit(FuelImportRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(DkvImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DkvImportPreviewDto>> Preview(
        [FromForm] DkvImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();

        return Ok(await Mediator.Send(
            new PreviewDkvImportCommand
            {
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content
            },
            cancellationToken));
    }

    /// <summary>Stores the statement and pairs each row with the driver's entry. Safe to repeat.</summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-transactions/import")]
    [HttpPost("/api/fuel-transactions/import")]
    [RequestSizeLimit(FuelImportRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(DkvImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DkvImportResultDto>> Import(
        [FromForm] DkvImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();

        return Ok(await Mediator.Send(
            new ImportDkvStatementCommand
            {
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content
            },
            cancellationToken));
    }

    /// <summary>The driver entries a row could be paired with by hand.</summary>
    [HttpGet("/api/v{version:apiVersion}/fuel-transactions/{id:guid}/candidates")]
    [HttpGet("/api/fuel-transactions/{id:guid}/candidates")]
    [ProducesResponseType(typeof(List<FuelExpenseCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FuelExpenseCandidateDto>>> GetCandidates(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetFuelExpenseCandidatesQuery(id), cancellationToken));
    }

    /// <summary>Settles a row by hand: pair it, record it, confirm it as it is, or set it aside.</summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-transactions/{id:guid}/resolve")]
    [HttpPost("/api/fuel-transactions/{id:guid}/resolve")]
    [ProducesResponseType(typeof(FuelTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FuelTransactionDto>> Resolve(
        Guid id,
        ResolveFuelTransactionCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Puts a card on a vehicle and re-checks every open row. Returns how many changed state.</summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-transactions/assign-card")]
    [HttpPost("/api/fuel-transactions/assign-card")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> AssignCard(
        AssignDkvCardCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Matches every open row again, e.g. after drivers have caught up. Returns how many changed state.</summary>
    [HttpPost("/api/v{version:apiVersion}/fuel-transactions/recheck")]
    [HttpPost("/api/fuel-transactions/recheck")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> Recheck(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new RecheckDkvTransactionsCommand(), cancellationToken));
    }
}

/// <summary>The multipart body of a DKV statement upload.</summary>
public class DkvImportRequest
{
    public IFormFile? File { get; set; }
}
