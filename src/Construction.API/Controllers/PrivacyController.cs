using Construction.API.Authorization;
using Construction.Application.Features.Privacy.Commands.ErasePersonalData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Acting on a data-subject request.
/// </summary>
/// <remarks>
/// Super Admin only, and narrower than the rest of account administration on
/// purpose: erasure is irreversible and it is not an ordinary day's work. An
/// Admin can offboard somebody — deactivate the account, end the postings —
/// which is the reversible operation and the one that gets used weekly.
/// </remarks>
[Authorize(Policy = Policies.SuperAdminOnly)]
public class PrivacyController : ApiControllerBase
{
    /// <summary>
    /// Erases an employee's personal data, keeping the employment record.
    /// </summary>
    /// <remarks>
    /// Removes the GPS track, clock-in coordinates, absence reasons, contact
    /// details, date of birth, notifications, device tokens and sessions;
    /// keeps hours, rates, the employee number and the fact of employment.
    ///
    /// Irreversible. The response reports what was removed, and the audit
    /// trail records who did it and why. Attachments are counted rather than
    /// deleted — their bytes live outside the database; see docs/PRIVACY.md.
    /// </remarks>
    [HttpPost("employees/{employeeId:guid}/erase")]
    [ProducesResponseType(typeof(ErasureResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ErasureResult>> Erase(
        Guid employeeId,
        [FromBody] ErasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ErasePersonalDataCommand
            {
                EmployeeId = employeeId,
                Reason = request.Reason
            },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>The body of an erasure request.</summary>
    /// <remarks>
    /// A record rather than a bare string parameter so the reason arrives as
    /// JSON like every other body, and so adding a second field later does not
    /// change the shape of the endpoint.
    /// </remarks>
    public record ErasureRequest
    {
        /// <summary>Why the erasure is being carried out. Recorded in the audit trail.</summary>
        public string Reason { get; init; } = string.Empty;
    }
}
