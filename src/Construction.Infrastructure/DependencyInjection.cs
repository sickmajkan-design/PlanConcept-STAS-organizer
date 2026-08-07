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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Whitespace, not just null. An empty string passes a null guard and
        // then fails somewhere inside Npgsql with a message about a missing
        // host — which sends whoever is reading it looking at the database
        // rather than at the environment variable nobody set.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Set "
                + "ConnectionStrings__DefaultConnection.");
        }

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

        // Validated on start, like JwtSettings. These used to be bound and
        // forgotten, so a wrong port or an unreadable credential file was
        // discovered at the moment somebody needed the feature — which for
        // email means a password reset that is accepted, logged as sent, and
        // never delivered.
        //
        // The checks only fire when the feature is configured at all. Leaving
        // SMTP unset is the normal case on a developer machine, and is caught
        // separately at startup for a deployment.
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SectionName))
            .Validate(
                s => !s.IsConfigured || s.Port is > 0 and <= 65535,
                "EmailSettings:Port must be between 1 and 65535.")
            .Validate(
                s => !s.IsConfigured || IsEmailAddress(s.FromAddress),
                "EmailSettings:FromAddress must be a valid email address; mail servers reject the message otherwise.")
            .ValidateOnStart();

        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddOptions<FirebaseSettings>()
            .Bind(configuration.GetSection(FirebaseSettings.SectionName))
            .Validate(
                s => string.IsNullOrWhiteSpace(s.CredentialsPath)
                    || string.IsNullOrWhiteSpace(s.CredentialsJson),
                "Firebase:CredentialsPath and Firebase:CredentialsJson are alternatives; set one, not both.")
            .Validate(
                s => string.IsNullOrWhiteSpace(s.CredentialsPath) || File.Exists(s.CredentialsPath),
                "Firebase:CredentialsPath points at a file that does not exist.")
            .Validate(
                s => string.IsNullOrWhiteSpace(s.CredentialsJson) || IsJsonObject(s.CredentialsJson),
                "Firebase:CredentialsJson is not valid JSON. A service-account key pasted into an environment variable is easily truncated or shell-mangled.")
            .ValidateOnStart();

        services.AddSingleton<IPushSender, FcmPushSender>();

        AddFileStorage(services, configuration);
    }

    /// <summary>
    /// Good enough to catch a typo, not an attempt at RFC 5321.
    /// </summary>
    /// <remarks>
    /// The point is to fail on "no-reply" or "no-reply@" at startup rather
    /// than on the first password reset. Anything stricter rejects addresses
    /// that are legal and in use.
    /// </remarks>
    private static bool IsEmailAddress(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && System.Net.Mail.MailAddress.TryCreate(value, out _);

    private static bool IsJsonObject(string value)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(value);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
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
