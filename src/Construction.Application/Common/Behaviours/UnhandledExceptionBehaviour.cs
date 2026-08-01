using Construction.Application.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Common.Behaviours;

/// <summary>
/// Logs unexpected exceptions with full request context before they bubble
/// up to the API exception-handling middleware. Known application exceptions
/// (validation, not-found, …) pass through untouched.
/// </summary>
public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehaviour(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A caller that hung up is not a fault. It still gets a line, so a
            // handler that keeps being abandoned is visible, but not one that
            // pages anyone at three in the morning.
            _logger.LogInformation(
                "Request {RequestName} was cancelled by the caller",
                typeof(TRequest).Name);
            throw;
        }
        catch (Exception ex) when (ex is not ValidationException
                                       and not NotFoundException
                                       and not UnauthorizedException
                                       and not ForbiddenAccessException
                                       and not ConflictException)
        {
            _logger.LogError(ex, "Unhandled exception for request {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }
}
