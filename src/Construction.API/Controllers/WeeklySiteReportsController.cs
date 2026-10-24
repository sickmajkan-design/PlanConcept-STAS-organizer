using System.Text.Json;
using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;
using Construction.Application.Features.WeeklySiteReports.Commands.UpdateWeeklySiteReportLines;
using Construction.Application.Features.WeeklySiteReports.Commands.MarkWeeklySiteReportProcessed;
using Construction.Application.Features.WeeklySiteReports.Commands.RequestWeeklySiteReportCorrection;
using Construction.Application.Features.WeeklySiteReports.Models;
using Construction.Application.Features.WeeklySiteReports.Queries.GetMyReportableProjects;
using Construction.Application.Features.WeeklySiteReports.Queries.GetMyWeeklySiteReports;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportContent;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReport;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReports;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportsSummary;
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

    /// <summary>What waits for the office: reports to review, reports sent back, sites that are late.</summary>
    [HttpGet("summary")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(WeeklySiteReportsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeeklySiteReportsSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetWeeklySiteReportsSummaryQuery(), cancellationToken));
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

    /// <summary>One report with its pictures and workers, and what the monthly sheet holds for each worker now.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(WeeklySiteReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySiteReportDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetWeeklySiteReportQuery(id), cancellationToken));
    }

    /// <summary>One picture of the report.</summary>
    [HttpGet("{id:guid}/pages/{pageId:guid}/content")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageContent(Guid id, Guid pageId, CancellationToken cancellationToken)
    {
        var file = await Mediator.Send(new GetWeeklySiteReportContentQuery(id, pageId), cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost]
    [RequestSizeLimit(205 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 205 * 1024 * 1024)]
    [ProducesResponseType(typeof(WeeklySiteReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WeeklySiteReportDto>> Create(
        [FromForm] CreateWeeklySiteReportRequest request,
        CancellationToken cancellationToken)
    {
        // Several pictures arrive as `files`; the single `file` of older clients is the same thing with one.
        var uploads = (request.Files ?? []).Concat(request.File is null ? [] : [request.File])
            .Where(f => f.Length > 0)
            .ToList();

        if (uploads.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Files), "A file is required.");
            return ValidationProblem(ModelState);
        }

        var lines = new List<ReportLineInput>();

        if (!string.IsNullOrWhiteSpace(request.Lines))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<ReportLineBody>>(
                    request.Lines, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

                lines.AddRange(parsed.Select(l => new ReportLineInput(l.EmployeeId, l.Hours, l.HoursSecondMonth)));
            }
            catch (JsonException)
            {
                ModelState.AddModelError(nameof(request.Lines), "The list of workers could not be read.");
                return ValidationProblem(ModelState);
            }
        }

        var streams = new List<Stream>();

        try
        {
            var files = new List<ReportFileInput>();

            foreach (var upload in uploads)
            {
                var stream = upload.OpenReadStream();
                streams.Add(stream);
                files.Add(new ReportFileInput(upload.FileName, upload.Length, stream));
            }

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
                    Files = files,
                    Lines = lines,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetContent), new { id = report.Id }, report);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Approves a report and enters its hours into the monthly sheet. The body, if any, carries the workers as the
    /// office has just checked them and the workers whose cell in the sheet is to stay as it is. The old name stays so
    /// clients written against it keep working.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [HttpPost("{id:guid}/mark-processed")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(ApproveWeeklySiteReportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApproveWeeklySiteReportResult>> MarkProcessed(
        Guid id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ApproveRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new MarkWeeklySiteReportProcessedCommand(
                id,
                request?.Lines?.Select(l => new ReportLineInput(l.EmployeeId, l.Hours, l.HoursSecondMonth)).ToList(),
                request?.KeepExisting),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Saves the office's corrections to the workers and hours without approving the report.</summary>
    [HttpPut("{id:guid}/lines")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLines(
        Guid id,
        [FromBody] UpdateLinesRequest request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new UpdateWeeklySiteReportLinesCommand(
                id,
                (request.Lines ?? []).Select(l => new ReportLineInput(l.EmployeeId, l.Hours, l.HoursSecondMonth)).ToList()),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Sends a report back to the foreman with a reason; the foreman sends it again.</summary>
    [HttpPost("{id:guid}/request-correction")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestCorrection(
        Guid id,
        [FromBody] RequestCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RequestWeeklySiteReportCorrectionCommand(id, request.Note ?? string.Empty), cancellationToken);
        return NoContent();
    }
}

/// <summary>One worker's hours for the report's week; the part that falls in the later month is given apart.</summary>
public class ReportLineBody
{
    public Guid EmployeeId { get; set; }

    public decimal Hours { get; set; }

    public decimal HoursSecondMonth { get; set; }
}

/// <summary>The workers as the office checked them, and who keeps what the sheet already says.</summary>
public class ApproveRequest
{
    public List<ReportLineBody>? Lines { get; set; }

    public List<Guid>? KeepExisting { get; set; }
}

public class UpdateLinesRequest
{
    public List<ReportLineBody>? Lines { get; set; }
}

/// <summary>Why a report is sent back.</summary>
public class RequestCorrectionRequest
{
    public string? Note { get; set; }
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

    /// <summary>The pictures of the signed sheet: a week can be written on several.</summary>
    public List<IFormFile>? Files { get; set; }

    /// <summary>The single file of older clients.</summary>
    public IFormFile? File { get; set; }

    /// <summary>The workers and their hours, as JSON: [{"employeeId":"…","hours":40,"hoursSecondMonth":0}].</summary>
    public string? Lines { get; set; }
}
