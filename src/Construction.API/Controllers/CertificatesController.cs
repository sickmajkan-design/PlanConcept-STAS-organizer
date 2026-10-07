using Construction.API.Authorization;
using Construction.Application.Features.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>What a worker is certified for, and until when. The schedule checks it against what a project requires.</summary>
[Authorize(Policy = Policies.ProjectManagerAndAbove)]
public class CertificatesController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/employees/{employeeId:guid}/certificates")]
    [HttpGet("/api/employees/{employeeId:guid}/certificates")]
    [ProducesResponseType(typeof(List<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CertificateDto>>> List(Guid employeeId, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetEmployeeCertificatesQuery(employeeId), cancellationToken));
    }

    /// <summary>Adds a certificate, or, with an id, changes the one the worker already has.</summary>
    [HttpPut("/api/v{version:apiVersion}/employees/{employeeId:guid}/certificates")]
    [HttpPut("/api/employees/{employeeId:guid}/certificates")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CertificateDto>> Save(
        Guid employeeId,
        [FromBody] SaveEmployeeCertificateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { EmployeeId = employeeId }, cancellationToken));
    }

    [HttpDelete("/api/v{version:apiVersion}/employees/{employeeId:guid}/certificates/{id:guid}")]
    [HttpDelete("/api/employees/{employeeId:guid}/certificates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteEmployeeCertificateCommand(employeeId, id), cancellationToken);
        return NoContent();
    }
}
