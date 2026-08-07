using System.Text.RegularExpressions;
using Serilog.Context;

namespace Construction.API.Middleware;

/// <summary>
/// Gives every request an id that appears in the logs, comes back in the
/// response, and travels with a call from one service to the next.
/// </summary>
/// <remarks>
/// <para>
/// The thing that turns "it broke this morning" into a log query. A user
/// reports a failure; without this the only way to find it is to guess at a
/// time window and a path. With it they quote one string.
/// </para>
/// <para>
/// The header is accepted from the caller so a chain of calls shares one id,
/// but it is validated first rather than trusted. A value straight out of a
/// request and into a log file is a log-injection vector: newlines forge log
/// entries, and a few kilobytes of junk in every line is its own denial of
/// service against whoever pays per gigabyte ingested. Anything that is not a
/// short, plain token is replaced rather than rejected — the caller gets a
/// working request and a usable id, just not the one they asked for.
/// </para>
/// </remarks>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Where the id is stashed for anything downstream that needs it.</summary>
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context);

        context.Items[ItemKey] = correlationId;

        // On the response before anything else writes to it: a middleware that
        // has already started the response cannot add headers, and the id is
        // most wanted on exactly the failures that write early.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Pushed for the duration of the request, so every log line written
        // while handling it carries the id — including Serilog's own
        // request-completed line, which is why this middleware runs first.
        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await _next(context);
        }
    }

    private static string Resolve(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();

        return IsSafe(supplied) ? supplied : Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// A short token of letters, digits, dashes and underscores.
    /// </summary>
    /// <remarks>
    /// Deliberately narrower than "anything without a newline". A log line is
    /// read by people and parsed by machines, and the set of characters that
    /// confuse one or the other is longer than it looks.
    /// </remarks>
    private static bool IsSafe(string value) =>
        value.Length is > 0 and <= 64 && SafeToken().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex SafeToken();
}
