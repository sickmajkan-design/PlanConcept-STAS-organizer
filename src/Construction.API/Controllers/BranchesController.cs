using Construction.API.Authorization;
using Construction.Application.Features.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The operator's own business units (poslovne jedinice). Anyone on the office side reads them
/// to filter by; only management changes them (see <c>BranchRules</c>).
/// </summary>
[Authorize(Policy = Policies.ForemanAndAbove)]
public class BranchesController : ApiControllerBase
{
    /// <summary>Lists the business units, active first.</summary>
    [HttpGet("/api/v{version:apiVersion}/branches")]
    [HttpGet("/api/branches")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetBranchesQuery(), cancellationToken));
    }

    /// <summary>Adds a business unit. Admin and above.</summary>
    [HttpPost("/api/v{version:apiVersion}/branches")]
    [HttpPost("/api/branches")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> Create(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { id = branch.Id }, branch);
    }

    /// <summary>Renames, recolours or switches a business unit off or on. Admin and above.</summary>
    [HttpPut("/api/v{version:apiVersion}/branches/{id:guid}")]
    [HttpPut("/api/branches/{id:guid}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> Update(Guid id, UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Streams a unit's logo. No token: a picture in the page cannot send one. Dark falls back to light.</summary>
    [HttpGet("/api/v{version:apiVersion}/branches/{id:guid}/logo")]
    [HttpGet("/api/branches/{id:guid}/logo")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo(
        Guid id,
        [FromQuery] bool dark,
        [FromQuery] bool raster,
        CancellationToken cancellationToken)
    {
        var logo = await Mediator.Send(
            new GetBranchLogoQuery(id, dark ? BranchLogoVariant.Dark : BranchLogoVariant.Light, raster),
            cancellationToken);

        if (logo is null)
        {
            return NotFound();
        }

        LogoResponse.Harden(Response, logo.ContentType);

        return File(logo.Content, logo.ContentType);
    }

    /// <summary>Sets or replaces a unit's logo (png, jpg or webp, 2 MB). Admin and above.</summary>
    [HttpPost("/api/v{version:apiVersion}/branches/{id:guid}/logo")]
    [HttpPost("/api/branches/{id:guid}/logo")]
    [RequestSizeLimit(BranchLogoRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchDto>> UploadLogo(
        Guid id,
        [FromQuery] bool dark,
        [FromForm] UploadBranchLogoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();
        await using var rasterContent = request.Raster is { Length: > 0 } ? request.Raster.OpenReadStream() : null;

        return Ok(await Mediator.Send(
            new UploadBranchLogoCommand
            {
                Id = id,
                Variant = dark ? BranchLogoVariant.Dark : BranchLogoVariant.Light,
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content,
                RasterContent = rasterContent,
            },
            cancellationToken));
    }

    /// <summary>Removes a unit's logo (removing the light one removes the dark one too). Admin and above.</summary>
    [HttpDelete("/api/v{version:apiVersion}/branches/{id:guid}/logo")]
    [HttpDelete("/api/branches/{id:guid}/logo")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchDto>> DeleteLogo(Guid id, [FromQuery] bool dark, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(
            new DeleteBranchLogoCommand(id, dark ? BranchLogoVariant.Dark : BranchLogoVariant.Light),
            cancellationToken));
    }

    /// <summary>Makes a unit the one whose logo stands for "all business units". Admin and above.</summary>
    [HttpPut("/api/v{version:apiVersion}/branches/{id:guid}/main")]
    [HttpPut("/api/branches/{id:guid}/main")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> SetMain(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new SetMainBranchCommand(id), cancellationToken));
    }

    /// <summary>
    /// Sets the Main projects of a business unit (their sub-projects follow). Projects that were in
    /// it and are not listed are released. Admin and above.
    /// </summary>
    [HttpPut("/api/v{version:apiVersion}/branches/{id:guid}/projects")]
    [HttpPut("/api/branches/{id:guid}/projects")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchDto>> SetProjects(
        Guid id,
        SetBranchProjectsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Moves several employees into a business unit from a date. Everyone else keeps their unit.
    /// All or nothing. Admin and above.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/branches/{id:guid}/employees")]
    [HttpPost("/api/branches/{id:guid}/employees")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> AssignEmployees(
        Guid id,
        AssignEmployeesToBranchCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Deletes a business unit with no projects. Admin and above.</summary>
    [HttpDelete("/api/v{version:apiVersion}/branches/{id:guid}")]
    [HttpDelete("/api/branches/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteBranchCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>The multipart form a unit logo upload arrives as.</summary>
public class UploadBranchLogoRequest
{
    public IFormFile? File { get; set; }

    /// <summary>For an SVG, a png copy of it that spreadsheets and the phone draw.</summary>
    public IFormFile? Raster { get; set; }
}

/// <summary>Headers for a logo served by its own address, so an SVG cannot do anything there.</summary>
public static class LogoResponse
{
    public static void Harden(HttpResponse response, string contentType)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";

        if (contentType == Application.Features.Branches.LogoStorage.SvgContentType)
        {
            response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
        }
    }
}
