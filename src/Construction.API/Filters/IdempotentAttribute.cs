using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Construction.API.Filters;

/// <summary>
/// Makes an endpoint safe to retry: a second request carrying the same
/// <c>Idempotency-Key</c> is answered with the first one's response instead of
/// being carried out again.
/// </summary>
/// <remarks>
/// Put this on anything whose effect is relative or additive — a stock
/// movement, a cost row — where running it twice means two of it. It is not
/// needed on a PUT that sets absolute values, which is already idempotent by
/// construction.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : TypeFilterAttribute
{
    public IdempotentAttribute()
        : base(typeof(IdempotencyFilter))
    {
    }
}

/// <summary>The work behind <see cref="IdempotentAttribute"/>.</summary>
public sealed class IdempotencyFilter : IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    /// <summary>Set on a response that was replayed rather than performed.</summary>
    public const string ReplayHeaderName = "Idempotent-Replay";

    private const int MinimumKeyLength = 8;
    private const int MaximumKeyLength = 128;

    private readonly IIdempotencyStore _store;
    private readonly ICurrentUserService _currentUser;
    private readonly JsonSerializerOptions _json;
    private readonly ILogger<IdempotencyFilter> _logger;

    public IdempotencyFilter(
        IIdempotencyStore store,
        ICurrentUserService currentUser,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions,
        ILogger<IdempotencyFilter> logger)
    {
        _store = store;
        _currentUser = currentUser;
        _json = jsonOptions.Value.JsonSerializerOptions;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers[HeaderName].ToString();
        var userId = _currentUser.UserId;

        // No key means no guarantee, and the request runs as it always did.
        //
        // Requiring one would be the stronger design and it is not the one
        // here: it would reject every client written before this existed,
        // including integrations we do not control, and turn a retry problem
        // into an outage. Both of our own clients send a key — see
        // `idempotencyKey` in the admin panel and `withIdempotencyKey` in the
        // mobile app.
        if (string.IsNullOrEmpty(key) || userId is null)
        {
            await next();
            return;
        }

        if (key.Length is < MinimumKeyLength or > MaximumKeyLength)
        {
            context.Result = Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid idempotency key",
                $"'{HeaderName}' must be between {MinimumKeyLength} and {MaximumKeyLength} characters.");

            return;
        }

        var endpoint = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        var hash = Fingerprint(context);

        var claim = await _store.ClaimAsync(
            userId.Value,
            key,
            endpoint,
            hash,
            context.HttpContext.RequestAborted);

        switch (claim.Outcome)
        {
            case IdempotencyOutcome.Replay:
                _logger.LogInformation(
                    "Replayed idempotent request {Endpoint} for key {IdempotencyKey}",
                    endpoint,
                    key);

                context.HttpContext.Response.Headers[ReplayHeaderName] = "true";

                context.Result = new ContentResult
                {
                    StatusCode = claim.StatusCode ?? StatusCodes.Status200OK,
                    Content = claim.ResponseBody,
                    ContentType = "application/json",
                };

                return;

            case IdempotencyOutcome.InFlight:
                // The honest answer. The first attempt has not finished, so
                // there is nothing to replay and running it again is the one
                // thing this endpoint must not do.
                context.Result = Problem(
                    context,
                    StatusCodes.Status409Conflict,
                    "Request in progress",
                    "A request with this idempotency key is still being processed. Retry shortly.");

                return;

            case IdempotencyOutcome.Mismatch:
                _logger.LogWarning(
                    "Idempotency key {IdempotencyKey} reused for a different request on {Endpoint}",
                    key,
                    endpoint);

                context.Result = Problem(
                    context,
                    StatusCodes.Status422UnprocessableEntity,
                    "Idempotency key reused",
                    "This idempotency key has already been used for a different request. Generate a new one per action.");

                return;
        }

        var executed = await next();

        var status = StatusOf(executed);

        // Only a success is remembered. A failure that is stored would be
        // handed back for ever — including the transient 500 the retry was
        // meant to get past, which is the reason the client is retrying at
        // all.
        if (executed.Exception is not null || status is null or < 200 or >= 300)
        {
            await _store.ReleaseAsync(userId.Value, key, CancellationToken.None);
            return;
        }

        var body = executed.Result is ObjectResult { Value: not null } result
            ? JsonSerializer.Serialize(result.Value, _json)
            : null;

        // CancellationToken.None on purpose: the client hanging up is exactly
        // when this matters. The work is already committed, and a record left
        // incomplete would make the retry — the one that reconnects — collide
        // with an in-flight key rather than being answered.
        await _store.CompleteAsync(userId.Value, key, status.Value, body, CancellationToken.None);
    }

    /// <summary>
    /// What the key was first used for: the route, and the arguments the
    /// action was bound with.
    /// </summary>
    /// <remarks>
    /// The bound arguments rather than the raw body, because the body would
    /// have to be buffered and re-read, and because two bodies differing only
    /// in whitespace are the same request. The cancellation token is dropped —
    /// it is an argument in name only, and it does not serialise.
    /// </remarks>
    private string Fingerprint(ActionExecutingContext context)
    {
        var arguments = context.ActionArguments
            .Where(pair => pair.Value is not CancellationToken)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var payload = string.Concat(
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            JsonSerializer.Serialize(arguments, _json));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static int? StatusOf(ActionExecutedContext context) => context.Result switch
    {
        ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
        StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
        _ => null,
    };

    private static ObjectResult Problem(
        ActionContext context,
        int status,
        string title,
        string detail)
    {
        var factory = context.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>();

        var problem = factory.CreateProblemDetails(
            context.HttpContext,
            statusCode: status,
            title: title,
            detail: detail);

        return new ObjectResult(problem) { StatusCode = status };
    }
}
