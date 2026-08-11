using System.ComponentModel.DataAnnotations;
using Construction.API.Extensions;
using Construction.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Controllers;

/// <summary>
/// What the admin panel and the mobile app send when they break.
/// </summary>
/// <remarks>
/// <para>
/// Server faults have been findable for a while: a correlation id on every
/// request, in every log line, on the response and in the problem-details
/// body. Client faults had nowhere to go at all. A panel that shows "something
/// went wrong" and a phone that shows a crash panel are both telling one
/// person something nobody else will ever hear — and the failures that matter
/// most on a construction site happen on a phone, in a place the developer
/// will never stand.
/// </para>
/// <para>
/// This is deliberately not a third-party error service. The stack traces and
/// screen names of a workforce system describe where employees are and what
/// they were doing; sending that to somebody else's servers is a data-transfer
/// decision, not a monitoring one, and it belongs to the operator rather than
/// to a default. Reports land in the same Serilog pipeline as everything else,
/// so they reach whatever aggregator §5 of PROVISIONING is pointed at — on the
/// operator's own host, under the retention they already chose.
/// </para>
/// </remarks>
/// <remarks>
/// The routes are spelled out because the <c>[controller]</c> token would make
/// this <c>/api/v1/clienterrors</c>, and a run-together word is a path people
/// mistype. Hyphens are already how this API writes multi-word segments —
/// <c>forgot-password</c>, <c>reset-password</c> — so this follows that rather
/// than the token's default.
/// </remarks>
[Route("api/v{version:apiVersion}/client-errors")]
[Route("api/client-errors")]
public class ClientErrorsController : ApiControllerBase
{
    private readonly ILogger<ClientErrorsController> _logger;

    public ClientErrorsController(ILogger<ClientErrorsController> logger)
    {
        _logger = logger;
    }

    /// <summary>Records one client-side failure.</summary>
    /// <remarks>
    /// <para>
    /// Anonymous, because the failure this most needs to hear about is the one
    /// on the sign-in screen — a panel that cannot authenticate cannot report
    /// that it cannot authenticate. Rate limited for the same reason it is
    /// anonymous: a crash loop is a client sending this as fast as it can, and
    /// an endpoint that writes a log line per call is a way to fill a disk.
    /// </para>
    /// <para>
    /// Answers 202 rather than 200. Nothing downstream depends on the report
    /// having been stored, and a client that retries a failed error report is
    /// a client amplifying its own outage.
    /// </para>
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.ClientErrorPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult Report([FromBody] ClientErrorReport report)
    {
        // Logged as one event with named fields rather than an interpolated
        // sentence, so an aggregator can group by app and kind instead of
        // matching text. The message is a parameter, never part of the
        // template: it comes from a client, and a client that can choose the
        // template can forge log structure.
        _logger.LogError(
            "Client error in {ClientApp} {ClientVersion} on {ClientPlatform} at {ClientRoute}: "
            + "{ClientErrorKind}: {ClientErrorMessage}{ClientStack}",
            report.App,
            report.Version ?? "unknown",
            report.Platform ?? "unknown",
            report.Route ?? "unknown",
            report.Kind ?? "Error",
            report.Message,
            report.Stack is null ? string.Empty : Environment.NewLine + report.Stack);

        // The id this request already carries. A person reading the panel's
        // message and a person reading the log are then quoting the same
        // string, which is the entire point of having one.
        var correlationId = HttpContext.Items.TryGetValue(
            CorrelationIdMiddleware.ItemKey, out var value) ? value as string : null;

        return Accepted(new { correlationId });
    }
}

/// <summary>One failure, as a client can describe it.</summary>
/// <remarks>
/// Every field is bounded. This is an unauthenticated endpoint that writes to
/// the log, so the size of what it will write down is part of its security,
/// not part of its ergonomics — a stack trace is worth keeping, a megabyte of
/// one is an attack.
/// </remarks>
public class ClientErrorReport
{
    /// <summary>Which client — the panel or the phone.</summary>
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string App { get; init; } = null!;

    /// <summary>The exception message, or whatever the platform gave.</summary>
    [Required]
    [StringLength(2_000, MinimumLength = 1)]
    public string Message { get; init; } = null!;

    /// <summary>Exception type, error name, or similar.</summary>
    [StringLength(200)]
    public string? Kind { get; init; }

    /// <summary>
    /// The stack, truncated by the client and again here.
    /// </summary>
    /// <remarks>
    /// Ten thousand characters is far more than a useful trace and far less
    /// than a problem. A trace longer than this has usually recursed, and the
    /// top of it is the part that says why.
    /// </remarks>
    [StringLength(10_000)]
    public string? Stack { get; init; }

    /// <summary>Screen or route the failure happened on.</summary>
    [StringLength(300)]
    public string? Route { get; init; }

    /// <summary>App version, so a fixed fault stops being reported.</summary>
    [StringLength(60)]
    public string? Version { get; init; }

    /// <summary>Android, iOS, or the browser's own description.</summary>
    [StringLength(200)]
    public string? Platform { get; init; }
}
