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

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));
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
