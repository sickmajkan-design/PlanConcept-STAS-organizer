using Construction.API.Authorization;
using Construction.Application.Features.CompanySettings.Commands;
using Construction.Application.Features.CompanySettings.Models;
using Construction.Application.Features.CompanySettings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The platform's own company profile — name, address, tax details, contact
/// info and a logo. A singleton, not a per-record resource: there is one
/// company using this app, so there is no id in any of these routes.
/// </summary>
/// <remarks>
/// Reading the full profile is open to any signed-in role; only writing it
/// is SuperAdmin-gated. The <c>branding</c> and logo GET routes are the one
/// deliberate <c>[AllowAnonymous]</c> carve-out in this controller — the
/// login screen renders before there is a token to send, and a company logo
/// is not sensitive.
/// </remarks>
public class CompanySettingsController : ApiControllerBase
{
    // Explicit absolute routes throughout, matching CostsController and
    // ExportsController: the base `[Route("api/[controller]")]` on
    // ApiControllerBase takes the controller's literal name, and "company-settings"
    // is not "CompanySettings" — there is no slugifying route convention in
    // this project.

    [HttpGet("/api/v{version:apiVersion}/company-settings")]
    [HttpGet("/api/company-settings")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanySettingsDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetCompanySettingsQuery(), cancellationToken));
    }

    [HttpPut("/api/v{version:apiVersion}/company-settings")]
    [HttpPut("/api/company-settings")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CompanySettingsDto>> Update(
        UpdateCompanySettingsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>The time zones an organisation can choose between. Only whoever may change the setting needs it.</summary>
    [HttpGet("/api/v{version:apiVersion}/company-settings/time-zones")]
    [HttpGet("/api/company-settings/time-zones")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetTimeZones() => Ok(Application.Common.CompanyTimeZones.All);

    /// <summary>Name, whether a logo exists and the time zone — nothing else sensitive. No token required.</summary>
    [HttpGet("/api/v{version:apiVersion}/company-settings/branding")]
    [HttpGet("/api/company-settings/branding")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicCompanyBrandingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicCompanyBrandingDto>> GetBranding(CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetPublicCompanyBrandingQuery(), cancellationToken));
    }

    /// <summary>Streams the logo image. No token required — it must render before login.</summary>
    [HttpGet("/api/v{version:apiVersion}/company-settings/logo")]
    [HttpGet("/api/company-settings/logo")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo([FromQuery] bool raster, CancellationToken cancellationToken)
    {
        var logo = await Mediator.Send(new GetCompanyLogoQuery(raster), cancellationToken);

        if (logo is null)
        {
            return NotFound();
        }

        LogoResponse.Harden(Response, logo.ContentType);

        return File(logo.Content, logo.ContentType);
    }

    [HttpPost("/api/v{version:apiVersion}/company-settings/logo")]
    [HttpPost("/api/company-settings/logo")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [RequestSizeLimit(Application.Features.Branches.LogoStorage.MaxSizeBytes * 2 + 1024 * 1024)]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CompanySettingsDto>> UploadLogo(
        [FromForm] UploadCompanyLogoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        await using var content = request.File.OpenReadStream();
        await using var rasterContent = request.Raster is { Length: > 0 } ? request.Raster.OpenReadStream() : null;

        var settings = await Mediator.Send(
            new UploadCompanyLogoCommand
            {
                FileName = request.File.FileName,
                SizeBytes = request.File.Length,
                Content = content,
                RasterContent = rasterContent
            },
            cancellationToken);

        return Ok(settings);
    }

    [HttpDelete("/api/v{version:apiVersion}/company-settings/logo")]
    [HttpDelete("/api/company-settings/logo")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteLogo(CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteCompanyLogoCommand(), cancellationToken);
        return NoContent();
    }
}

/// <summary>The multipart form a logo upload arrives as.</summary>
public class UploadCompanyLogoRequest
{
    public IFormFile? File { get; set; }

    /// <summary>For an SVG, a png copy of it that spreadsheets and the phone draw.</summary>
    public IFormFile? Raster { get; set; }
}
