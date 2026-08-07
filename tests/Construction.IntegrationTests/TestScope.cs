using Construction.Application.Common.Interfaces;
using Construction.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.IntegrationTests;

/// <summary>
/// One resolved scope, standing in for a single HTTP request. Sending through
/// MediatR rather than calling handlers directly keeps the validation and
/// logging behaviours in the path, so a test exercises what the API exercises.
/// </summary>
public sealed class TestScope : IDisposable
{
    private readonly IServiceScope _scope;

    public TestScope(IServiceScope scope)
    {
        _scope = scope;
    }

    public IMediator Mediator => _scope.ServiceProvider.GetRequiredService<IMediator>();

    public ApplicationDbContext Db => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    public TestCurrentUserService CurrentUser =>
        _scope.ServiceProvider.GetRequiredService<TestCurrentUserService>();

    public MutableDateTimeProvider Clock =>
        _scope.ServiceProvider.GetRequiredService<MutableDateTimeProvider>();

    public RecordingEmailSender Emails =>
        _scope.ServiceProvider.GetRequiredService<RecordingEmailSender>();

    public RecordingPushSender Pushes =>
        _scope.ServiceProvider.GetRequiredService<RecordingPushSender>();

    public IOutbox Outbox => _scope.ServiceProvider.GetRequiredService<IOutbox>();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
        Mediator.Send(request);

    /// <summary>Overload for commands that return nothing, such as logout.</summary>
    public Task Send(IRequest request) => Mediator.Send(request);

    public void Dispose() => _scope.Dispose();
}
