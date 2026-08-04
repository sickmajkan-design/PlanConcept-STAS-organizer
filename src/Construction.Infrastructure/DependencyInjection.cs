using Construction.Application.Common.Interfaces;
using Construction.Infrastructure.Spreadsheets;
using Construction.Application.Features.Authentication.Commands.ForgotPassword;
using Construction.Infrastructure.Authentication;
using Construction.Infrastructure.Email;
using Construction.Infrastructure.Notifications;
using Construction.Infrastructure.Persistence;
using Construction.Infrastructure.Persistence.Interceptors;
using Construction.Infrastructure.Services;
using Construction.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddAuthentication(services, configuration);
        AddServices(services, configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3));

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<DbInitializer>();
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.SecretKey) && s.SecretKey.Length >= 32,
                "JwtSettings:SecretKey must be configured and at least 32 characters long.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer), "JwtSettings:Issuer must be configured.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "JwtSettings:Audience must be configured.")
            .ValidateOnStart();

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IResetLinkBuilder, ResetLinkBuilder>();

        // Validating incoming bearer tokens is a web-host concern and lives in
        // the API layer (AddJwtBearerAuthentication). Infrastructure only owns
        // how tokens are issued and how credentials are stored.
    }

    private static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Stateless: it turns a value object into bytes and holds nothing.
        services.AddSingleton<ISpreadsheetWriter, ClosedXmlSpreadsheetWriter>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.Configure<FirebaseSettings>(configuration.GetSection(FirebaseSettings.SectionName));
        services.AddSingleton<IPushSender, FcmPushSender>();

        AddFileStorage(services, configuration);
    }

    /// <summary>
    /// Picks the storage backing from configuration: a bucket means object
    /// storage, anything else means the local disk.
    /// </summary>
    /// <remarks>
    /// Defaulting to the disk keeps a fresh clone and CI runnable with no
    /// external service, and the disk implementation logs loudly enough that
    /// nobody reaches production on it by accident. Choosing on the presence
    /// of a bucket rather than on a separate "provider" setting removes the
    /// state where the two disagree.
    /// </remarks>
    private static void AddFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(FileStorageSettings.SectionName);

        services.Configure<FileStorageSettings>(section);

        var settings = section.Get<FileStorageSettings>() ?? new FileStorageSettings();

        if (settings.UsesObjectStorage)
        {
            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            services.AddSingleton<IFileStorage, FileSystemFileStorage>();
        }
    }
}
