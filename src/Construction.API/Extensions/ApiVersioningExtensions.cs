using Asp.Versioning;

namespace Construction.API.Extensions;

/// <summary>
/// Puts a version in the URL, so a breaking change has somewhere to live.
/// </summary>
/// <remarks>
/// <para>
/// The reason to do this before release rather than after: once the mobile app
/// is in an app store, there is no way to make everyone update. A phone on a
/// site runs whatever version its owner last installed, and the first change
/// that alters a response shape breaks it silently — a screen that stops
/// filling in, reported weeks later as "the app is broken". With versioned
/// routes the old build keeps calling <c>/api/v1</c>, which keeps behaving the
/// way it did the day it shipped, and the new one calls <c>/api/v2</c>.
/// </para>
/// <para>
/// Retrofitting this later means changing every route in the API and every
/// call site in two clients while both are live. Now it is a prefix.
/// </para>
/// </remarks>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// The version an unversioned request gets.
    /// </summary>
    /// <remarks>
    /// <strong>This must never change.</strong> The unversioned routes are
    /// kept as permanent aliases, so <c>/api/employees</c> means "version 1"
    /// and nothing else, forever. Bumping this would silently move every
    /// client that has not been updated onto a version it was never written
    /// for — which is precisely the failure versioning exists to prevent, and
    /// it would arrive as changed behaviour rather than as an error. A test
    /// asserts it.
    /// </remarks>
    public static readonly ApiVersion Default = new(1, 0);

    public static IServiceCollection AddApplicationApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = Default;

                // So the routes that existed before this change keep working.
                // Nothing in flight breaks, and a client updates when it
                // updates rather than because the server moved.
                options.AssumeDefaultVersionWhenUnspecified = true;

                // Advertises what this server speaks, so a client can find out
                // without reading the documentation for a deployment it might
                // not be looking at.
                options.ReportApiVersions = true;

                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
