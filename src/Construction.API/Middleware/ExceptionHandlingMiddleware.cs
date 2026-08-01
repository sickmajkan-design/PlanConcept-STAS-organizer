using Construction.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Middleware;

/// <summary>
/// Translates application exceptions into RFC 7807 problem-details responses.
/// Unknown exceptions become opaque 500s — internals are never leaked to clients.
/// </summary>
public class ExceptionHandlingMiddleware
{
    /// <summary>Non-standard but widely understood: the client closed the request.</summary>
    private const int ClientClosedRequest = 499;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller went away mid-request — a user navigating off a page,
            // a closed tab, a client that timed out. Nothing failed on the
            // server and nobody is left to answer, so this must not be logged
            // as an error: otherwise ordinary navigation raises the 5xx rate
            // that availability alerts are built on.
            _logger.LogInformation(
                "Request aborted by the client for {Path}",
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                // 499 is the established convention for a client-closed
                // request, and keeps these out of the 5xx bucket.
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                // The status code and headers are already on the wire, so the
                // response cannot be rewritten. Logging and re-throwing lets
                // the server abort the connection rather than throw a second,
                // confusing exception on top of the first.
                _logger.LogError(
                    exception,
                    "Exception after the response started for {Path}; connection will be aborted",
                    context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                problemDetails = new ValidationProblemDetails(validationException.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };
                break;

            case UnauthorizedException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
                };
                break;

            case ForbiddenAccessException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
                };
                break;

            case NotFoundException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
                };
                break;

            case ConflictException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
                };
                break;

            default:
                _logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                };
                break;
        }

        // Correlates a client-reported failure with the server log entry; the
        // opaque 500 above is otherwise untraceable once a user reports it.
        problemDetails.Extensions["traceId"] =
            System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = problemDetails.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync<object>(problemDetails);
    }
}
