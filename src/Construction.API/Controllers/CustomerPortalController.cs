using Construction.API.Authorization;
using Construction.Application.Features.CustomerPortal.Models;
using Construction.Application.Features.CustomerPortal.Queries.GetMyCustomerProjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The one door an external client's login can open: their own project's
/// status, read-only. Everything else in the API is staff-only — see the
/// remarks on <see cref="Policies.AllEmployees"/> for why a customer login
/// does not fall through to any of it by accident.
/// </summary>
[Authorize(Policy = Policies.CustomerOnly)]
public class CustomerPortalController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/customer-portal/projects")]
    [HttpGet("/api/customer-portal/projects")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerProjectStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerProjectStatusDto>>> GetMyProjects(
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetMyCustomerProjectsQuery(), cancellationToken));
    }
}
