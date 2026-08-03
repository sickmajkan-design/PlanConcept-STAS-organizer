using Construction.API.Authorization;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.DeleteAttachment;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Models;
using Construction.Application.Features.Attachments.Queries.GetAttachmentContent;
using Construction.Application.Features.Attachments.Queries.GetAttachments;
using Construction.Application.Features.Attachments.Queries.GetExpiringDocuments;
using Construction.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Files attached to employees, projects, vehicles and tools.
/// </summary>
/// <remarks>
/// The route policies are deliberately loose — every signed-in employee may
/// reach these — because who may see what depends on the record a file hangs
/// off, not on the endpoint. An employee reads their own certificates; nobody
/// else's employee files leave Admin. That decision lives in
/// <see cref="AttachmentRules"/>, where it can be stated once and tested.
/// </remarks>
public class AttachmentsController : ApiControllerBase
{
    /// <summary>Lists the files on one record.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetList(
        [FromQuery] GetAttachmentsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Documents that have lapsed or are about to.</summary>
    [HttpGet("expiring")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetExpiring(
        [FromQuery] GetExpiringDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Streams one attachment's contents.</summary>
    [HttpGet("{id:guid}/content")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        var file = await Mediator.Send(new GetAttachmentContentQuery(id), cancellationToken);

        // `attachment` rather than inline: the stored type is derived from the
        // extension and the bytes are never inspected, so a file that claims
        // to be a PNG and is not must not be handed to the browser to render.
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Attaches a file to a record.</summary>
    /// <remarks>
    /// Multipart rather than JSON, and the stream is handed to storage as it
    /// arrives — a 20 MB file base64-encoded into a JSON body would be read
    /// into memory twice before anything touched a disk.
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = Policies.AllEmployees)]
    [RequestSizeLimit(AttachmentRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentDto>> Upload(
        [FromForm] UploadAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();

        var attachment = await Mediator.Send(
            new UploadAttachmentCommand
            {
                OwnerType = request.OwnerType,
                OwnerId = request.OwnerId,
                Category = request.Category,
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content,
                Description = request.Description,
                ExpiresAt = request.ExpiresAt
            },
            cancellationToken);

        return CreatedAtAction(nameof(GetContent), new { id = attachment.Id }, attachment);
    }

    /// <summary>Removes an attachment and the file behind it.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteAttachmentCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// The multipart form an upload arrives as.
/// </summary>
/// <remarks>
/// Separate from the command because <see cref="IFormFile"/> is an ASP.NET
/// type and the Application layer does not reference it: the command takes a
/// plain <see cref="Stream"/>, so a handler stays testable without a web host.
/// </remarks>
public class UploadAttachmentRequest
{
    public IFormFile? File { get; set; }

    public AttachmentOwnerType OwnerType { get; set; }

    public Guid OwnerId { get; set; }

    public AttachmentCategory Category { get; set; } = AttachmentCategory.Other;

    public string? Description { get; set; }

    public DateOnly? ExpiresAt { get; set; }
}
