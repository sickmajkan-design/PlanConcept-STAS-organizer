using Construction.Application.Common.Behaviours;
using Construction.Application.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Construction.UnitTests.Behaviours;

/// <summary>
/// The behaviour decides what reaches the error log. A caller that hangs up
/// must not look like a server fault, or ordinary page navigation inflates
/// the error rate that alerting is built on.
/// </summary>
public class UnhandledExceptionBehaviourTests
{
    private sealed record TestRequest : IRequest<string>;

    /// <summary>Captures what was logged, at what level, without a mocking library.</summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }

    private static (UnhandledExceptionBehaviour<TestRequest, string> Behaviour,
        RecordingLogger<UnhandledExceptionBehaviour<TestRequest, string>> Logger) Create()
    {
        var logger = new RecordingLogger<UnhandledExceptionBehaviour<TestRequest, string>>();
        return (new UnhandledExceptionBehaviour<TestRequest, string>(logger), logger);
    }

    [Fact]
    public async Task Passes_a_successful_response_through()
    {
        var (behaviour, logger) = Create();

        var result = await behaviour.Handle(
            new TestRequest(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task Logs_an_unexpected_failure_as_an_error()
    {
        var (behaviour, logger) = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => behaviour.Handle(
            new TestRequest(),
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None));

        Assert.Equal([LogLevel.Error], logger.Levels);
    }

    [Theory]
    [InlineData(typeof(ValidationException))]
    [InlineData(typeof(NotFoundException))]
    [InlineData(typeof(ConflictException))]
    [InlineData(typeof(UnauthorizedException))]
    [InlineData(typeof(ForbiddenAccessException))]
    public async Task Leaves_expected_application_exceptions_out_of_the_error_log(Type exceptionType)
    {
        var (behaviour, logger) = Create();

        var exception = exceptionType == typeof(ValidationException)
            ? new ValidationException()
            : (Exception)Activator.CreateInstance(exceptionType, "expected")!;

        await Assert.ThrowsAsync(exceptionType, () => behaviour.Handle(
            new TestRequest(),
            () => throw exception,
            CancellationToken.None));

        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task Treats_a_caller_cancellation_as_information_not_an_error()
    {
        var (behaviour, logger) = Create();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behaviour.Handle(
            new TestRequest(),
            () => throw new OperationCanceledException(cancellation.Token),
            cancellation.Token));

        Assert.Equal([LogLevel.Information], logger.Levels);
    }

    [Fact]
    public async Task Still_reports_a_cancellation_the_caller_did_not_ask_for()
    {
        // A handler that cancels its own work — a timeout inside the request,
        // say — is a genuine failure and has to stay visible as one.
        var (behaviour, logger) = Create();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behaviour.Handle(
            new TestRequest(),
            () => throw new OperationCanceledException(),
            CancellationToken.None));

        Assert.Equal([LogLevel.Error], logger.Levels);
    }
}
