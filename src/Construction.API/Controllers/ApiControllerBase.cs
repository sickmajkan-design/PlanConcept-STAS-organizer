using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
