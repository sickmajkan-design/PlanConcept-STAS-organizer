using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;
using Construction.Application.Features.WeeklySiteReports.Commands.MarkWeeklySiteReportProcessed;
using Construction.Application.Features.WeeklySiteReports.Models;
using Construction.Application.Features.WeeklySiteReports.Queries.GetMyReportableProjects;
using Construction.Application.Features.WeeklySiteReports.Queries.GetMyWeeklySiteReports;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportContent;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// A foreman's weekly proof-of-work for one site, sent to the office and
/// filed sorted by site and ISO week.
/// </summary>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class WeeklySiteReportsController : ApiControllerBase
{
    /// <summary>The office's inbox — Admin and above, same tier as every other cross-site review screen.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(PagedList<WeeklySiteReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<WeeklySiteReportDto>>> GetList(
        [FromQuery] GetWeeklySiteReportsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>What the caller has submitted themselves — the mobile app's own history.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedList<WeeklySiteReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<WeeklySiteReportDto>>> GetMine(
        [FromQuery] GetMyWeeklySiteReportsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Sites the caller may submit a report for.</summary>
    [HttpGet("reportable-projects")]
    [ProducesResponseType(typeof(IReadOnlyList<ReportableProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReportableProjectDto>>> GetReportableProjects(
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetMyReportableProjectsQuery(), cancellationToken));
    }

    /// <summary>The attached proof document.</summary>
    [HttpGet("{id:guid}/content")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        var file = await Mediator.Send(new GetWeeklySiteReportContentQuery(id), cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ProducesResponseType(typeof(WeeklySiteReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WeeklySiteReportDto>> Create(
        [FromForm] CreateWeeklySiteReportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();

        var report = await Mediator.Send(
            new CreateWeeklySiteReportCommand
            {
                ProjectId = request.ProjectId,
                IsoYear = request.IsoYear,
                IsoWeek = request.IsoWeek,
                Type = request.Type,
                SubmittedByEmployeeId = request.SubmittedByEmployeeId,
                Quantity = request.Quantity,
                Note = request.Note,
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content,
            },
            cancellationToken);

        return CreatedAtAction(nameof(GetContent), new { id = report.Id }, report);
    }

    /// <summary>Marks a report as handled — billed, filed, whatever "done" means to the office.</summary>
    [HttpPost("{id:guid}/mark-processed")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkProcessed(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new MarkWeeklySiteReportProcessedCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>The multipart form a submission arrives as.</summary>
public class CreateWeeklySiteReportRequest
{
    public Guid ProjectId { get; set; }

    public int IsoYear { get; set; }

    public int IsoWeek { get; set; }

    public Domain.Enums.WeeklyReportType Type { get; set; }

    public Guid? SubmittedByEmployeeId { get; set; }

    public decimal? Quantity { get; set; }

    public string? Note { get; set; }

    public IFormFile? File { get; set; }
}
