using Construction.Infrastructure.Email;
using Construction.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace Construction.API.Extensions;

/// <summary>
/// Fails a misconfigured deployment at startup instead of at the moment a user
/// needs the feature.
///
/// <para>
/// <c>JwtSettings</c> already validates itself through <c>ValidateOnStart</c>.
/// The settings checked here could not, because whether they are required
/// depends on the hosting environment: an unset SMTP host is the normal case
/// locally and a silent security failure in production, where every password
/// reset would be accepted, logged as sent, and never delivered.
/// </para>
/// </summary>
public static class StartupValidationExtensions
{
    /// <summary>Set to true to run without email, accepting that password reset will not work.</summary>
    public const string AllowUnconfiguredEmailKey = "EmailSettings:AllowUnconfigured";

    public static void ValidateProductionConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        var configuration = app.Configuration;
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Construction.API.StartupValidation");

        var failures = new List<string>();

        var email = app.Services.GetRequiredService<IOptions<EmailSettings>>().Value;

        if (!email.IsConfigured && !configuration.GetValue<bool>(AllowUnconfiguredEmailKey))
        {
            failures.Add(
                $"'{EmailSettings.SectionName}:Host' is not set, so no password-reset mail can be " +
                $"delivered. Configure SMTP, or set '{AllowUnconfiguredEmailKey}' to true to run " +
                "without password recovery deliberately.");
        }

        var resetUrl = configuration["ClientApp:PasswordResetUrl"];

        if (string.IsNullOrWhiteSpace(resetUrl))
        {
            failures.Add("'ClientApp:PasswordResetUrl' is not set; reset mail would link nowhere.");
        }
        else if (!Uri.TryCreate(resetUrl, UriKind.Absolute, out var uri))
        {
            failures.Add($"'ClientApp:PasswordResetUrl' is not an absolute URL: '{resetUrl}'.");
        }
        else if (uri.Scheme != Uri.UriSchemeHttps)
        {
            // The link carries a token equivalent to the password for an hour.
            failures.Add($"'ClientApp:PasswordResetUrl' must use https, not '{uri.Scheme}'.");
        }
        else if (uri.IsLoopback)
        {
            failures.Add(
                "'ClientApp:PasswordResetUrl' still points at localhost; users would be emailed " +
                "a link they cannot open.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "The configuration is not safe to run outside development:" +
                Environment.NewLine + " - " + string.Join(Environment.NewLine + " - ", failures));
        }

        // Warnings, not failures: these degrade a feature rather than a
        // security control, and some deployments legitimately go without them.
        if (configuration.GetSection(CorsOrigins.ConfigurationKey).Get<string[]>() is not { Length: > 0 })
        {
            // Deliberately not phrased as "the admin panel cannot reach the
            // API". It said that, and the deployment stack proved it wrong on
            // its first successful run: deploy/docker-compose.prod.yml serves
            // the panel and the API from one origin behind one proxy, so no
            // request is cross-origin and no CORS policy is consulted. An
            // operator reading the old sentence would go looking for a fault
            // that is not there, and the obvious way to silence it — listing
            // the installation's own origin — widens the policy for nothing.
            logger.LogWarning(
                "'{Key}' is empty, so any cross-origin browser request will be refused. That is " +
                "correct when the admin panel is served from this same origin, which is what the " +
                "deployment stack does. Set it only if the panel is served from its own name.",
                CorsOrigins.ConfigurationKey);
        }

        var firebase = app.Services.GetRequiredService<IOptions<FirebaseSettings>>().Value;

        if (!firebase.IsConfigured)
        {
            logger.LogWarning(
                "Firebase is not configured; push notifications will be recorded but never " +
                "delivered to a device.");
        }
    }
}
