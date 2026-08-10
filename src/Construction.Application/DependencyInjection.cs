using System.Reflection;
using Construction.Application.Common.Behaviours;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Authentication.Services;
using Construction.Application.Features.Notifications.Services;
using Construction.Application.Features.Outbox;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Nothing registers mapping any more. Each DTO carries its own
        // projection expression as a static field — see EmployeeMapping — so
        // there is no configuration to scan at start-up and no service to
        // inject. AutoMapper is gone: it is licensed under RPL-1.5, which
        // obliges publishing the source of software deployed to users, and its
        // only permissively licensed releases carry an unfixed high-severity
        // advisory. See the audit, C8.
        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Scoped, because enqueuing joins the caller's unit of work: the
        // message commits with the operation that caused it or not at all.
        services.AddScoped<IOutbox, OutboxWriter>();

        return services;
    }
}
