using System.Diagnostics;
using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Common.Behaviours;

/// <summary>
/// Logs every request going through the mediator, including the calling
/// user and the elapsed time. Warns when a request runs longer than 500 ms.
/// </summary>
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int LongRunningThresholdMs = 500;

    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "Handling {RequestName} for user {UserId}", requestName, userId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > LongRunningThresholdMs)
        {
            _logger.LogWarning(
                "Long-running request {RequestName} took {ElapsedMs} ms (user {UserId})",
                requestName, stopwatch.ElapsedMilliseconds, userId);
        }
        else
        {
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs} ms", requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
