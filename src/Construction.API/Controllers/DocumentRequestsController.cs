using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.DocumentRequests;
using Construction.Application.Features.DocumentRequests.Commands.CreateDocumentRequests;
using Construction.Application.Features.DocumentRequests.Commands.ReviewDocumentRequest;
using Construction.Application.Features.DocumentRequests.Commands.SubmitDocumentRequest;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequest;
using Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequests;
using Construction.Application.Features.DocumentRequests.Queries.GetMyDocumentRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The office asking people for documents that are missing, and people answering from the phone.
/// </summary>
/// <remarks>
/// The routes for the person are open to every signed-in employee because what they may reach is
/// decided by whose request it is, not by the endpoint: identity always comes from the token.
/// Asking and deciding are Admin and above.
/// </remarks>
public class DocumentRequestsController : ApiControllerBase
{
    /// <summary>The office's list of requests.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(PagedList<DocumentRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<DocumentRequestDto>>> GetList(
        [FromQuery] GetDocumentRequestsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>What the signed-in person has been asked for.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentRequestDto>>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(new GetMyDocumentRequestsQuery(), cancellationToken));

    /// <summary>One request with its history.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(DocumentRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentRequestDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(new GetDocumentRequestQuery(id), cancellationToken));

    /// <summary>Asks one or more people for one or more documents.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(CreateDocumentRequestsResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateDocumentRequestsResult>> Create(
        [FromBody] CreateDocumentRequestsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>The person answers a request with one document, as one or several files.</summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Policies.AllEmployees)]
    [RequestSizeLimit(
        AttachmentRules.MaxSizeBytes * DocumentRequestRules.MaxFilesPerSubmission + 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentRequestDto>> Submit(
        Guid id,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            ModelState.AddModelError(nameof(files), "Add at least one file.");
            return ValidationProblem(ModelState);
        }

        var opened = new List<(IFormFile File, Stream Stream)>();

        try
        {
            foreach (var file in files)
            {
                opened.Add((file, file.OpenReadStream()));
            }

            var result = await Mediator.Send(
                new SubmitDocumentRequestCommand
                {
                    Id = id,
                    Files = opened
                        .Select(o => new SubmittedFile(o.File.FileName, o.File.Length, o.Stream))
                        .ToList()
                },
                cancellationToken);

            return Ok(result);
        }
        finally
        {
            foreach (var (_, stream) in opened)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>Accepts the document that was sent.</summary>
    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(DocumentRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentRequestDto>> Accept(
        Guid id,
        [FromBody] AcceptDocumentRequestCommand command,
        CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(command with { Id = id }, cancellationToken));

    /// <summary>Sends the document back, with the reason.</summary>
    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(DocumentRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentRequestDto>> Return(
        Guid id,
        [FromBody] ReturnDocumentRequestCommand command,
        CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(command with { Id = id }, cancellationToken));

    /// <summary>Withdraws a request.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(DocumentRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentRequestDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(new CancelDocumentRequestCommand(id), cancellationToken));
}
