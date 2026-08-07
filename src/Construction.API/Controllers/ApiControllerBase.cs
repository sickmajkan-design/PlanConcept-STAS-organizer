using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// Every controller answers on two paths.
/// </summary>
/// <remarks>
/// <para>
/// <c>/api/v1/employees</c> is what a client should call: it says which
/// contract it was written against, and a future <c>/api/v2</c> can change
/// shape without touching it.
/// </para>
/// <para>
/// <c>/api/employees</c> is kept as a permanent alias for version 1, not as a
/// deprecation. Everything written before versioning existed calls it, and the
/// default version is pinned so it can never come to mean something else —
/// see <see cref="Extensions.ApiVersioningExtensions.Default"/>. Removing it
/// would break clients to no benefit; letting it float would break them
/// silently, which is worse.
/// </para>
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
