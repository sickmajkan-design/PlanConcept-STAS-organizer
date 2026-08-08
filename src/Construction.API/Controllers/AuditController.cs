using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Audit.Models;
using Construction.Application.Features.Audit.Queries.GetAuditTrail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Who changed what, and when.
/// </summary>
/// <remarks>
/// <para>
/// Read-only, and there is no endpoint to write or delete an entry. The trail
/// is written by a persistence interceptor rather than by any handler, so
/// there is nothing here for a caller to reach — an audit trail with a delete
/// endpoint answers a different question from the one it was built for.
/// </para>
/// <para>
/// Admin and above. The trail says where people were, what their pay rate is,
/// and who changed it, which is a broader view of the workforce than the
/// individual screens give — and a foreman who can see one employee's hours
/// has no reason to see every administrator's actions.
/// </para>
/// </remarks>
[Authorize(Policy = Policies.AdminAndAbove)]
public class AuditController : ApiControllerBase
{
    /// <summary>
    /// Lists recorded changes, newest first.
    /// </summary>
    /// <remarks>
    /// Filter by <c>entityName</c> and <c>entityId</c> to see one record's
    /// history, or by <c>userId</c> to see one person's actions.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<AuditEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<AuditEntryDto>>> GetList(
        [FromQuery] GetAuditTrailQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}
