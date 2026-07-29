using Construction.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Middleware;

/// <summary>
/// Translates application exceptions into RFC 7807 problem-details responses.
/// Unknown exceptions become opaque 500s — internals are never leaked to clients.
/// </summary>
public class ExceptionHandlingMiddleware
{
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
        catch (Exception exception)
        {
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

        context.Response.StatusCode = problemDetails.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync<object>(problemDetails);
    }
}
