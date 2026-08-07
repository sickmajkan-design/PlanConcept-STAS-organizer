using Construction.API.Authentication;
using Construction.API.Authorization;
using Construction.API.BackgroundServices;
using Construction.API.Extensions;
using Construction.API.Middleware;
using Construction.API.Services;
using Construction.Application;
using Construction.Application.Common.Interfaces;
using Construction.Infrastructure;
using Construction.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Construction Workforce Management API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.Configure<RefreshTokenCookieSettings>(
        builder.Configuration.GetSection(RefreshTokenCookieSettings.SectionName));
    builder.Services.AddSingleton<RefreshTokenCookie>();

    builder.Services.AddJwtBearerAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();
    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddAuthRateLimiting();

    builder.Services.AddTrustedProxyForwarding(builder.Configuration);

    builder.Services.AddTelemetry(builder.Configuration, builder.Environment);

    // Gives a body to the responses the framework writes itself. A 401 from
    // the authentication handler or a 403 from a policy never reaches
    // ExceptionHandlingMiddleware — they are status codes with nothing in
    // them, so the operator sees a generic message and has no id to quote.
    // This is the half of the correlation id that would otherwise only work
    // for failures a handler threw.
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            if (context.HttpContext.Items.TryGetValue(
                    CorrelationIdMiddleware.ItemKey, out var correlationId))
            {
                context.ProblemDetails.Extensions["correlationId"] = correlationId;
            }
        });

    // The product's recurring jobs. Both are safe to run on every replica: the
    // reminder sweep claims a row before notifying, so nobody is told twice,
    // and a deleted row cannot be deleted again.
    builder.Services.AddHostedService<DailyReminderService>();

    builder.Services.Configure<RetentionSettings>(
        builder.Configuration.GetSection(RetentionSettings.SectionName));
    builder.Services.AddHostedService<DataRetentionService>();

    // Sends what the request path queued. Nothing to claim first here either:
    // claiming a message moves it beyond its own lease, so a second worker
    // finds it no longer due.
    builder.Services.AddHostedService<OutboxService>();

    builder.Services.AddApplicationHealthChecks();

    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Default", policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Required for the browser to send and receive the refresh
            // cookie cross-origin. Safe here because the origins are named
            // rather than wildcarded — AllowCredentials with AllowAnyOrigin
            // is the combination ASP.NET Core refuses outright, and rightly.
            .AllowCredentials()
            // Response headers are hidden from cross-origin JavaScript unless
            // named here. Without this the admin panel cannot read the file
            // name off an export and every download arrives called
            // "export.xlsx" — which only shows up once the API and the panel
            // are on different origins, i.e. in production and not in dev.
            .WithExposedHeaders("Content-Disposition"));
    });

    var app = builder.Build();

    app.ValidateProductionConfiguration();

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync();
    }

    // Must run before anything reads the client address or scheme — the rate
    // limiter partitions on it and the refresh-token audit trail records it.
    app.UseTrustedProxyForwarding();

    // First of the application middleware, so every line logged for a request
    // carries its id — including Serilog's own request-completed line below.
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSecurityHeaders();

    // Request logging wraps the exception handler, not the other way round.
    // Inside it, Serilog would see every exception before it was translated
    // and record a 500 — so a duplicate employee number the client correctly
    // received as 409, or a page the user navigated away from, would land in
    // the log as a server fault and drive false alerts.
    app.UseSerilogRequestLogging(options =>
    {
        // The health endpoint is polled every few seconds by the orchestrator.
        // Logged at Information it is most of the log by volume and none of it
        // by value; at Debug it is there when somebody is looking for it.
        options.GetLevel = (context, _, exception) =>
            exception is not null
                ? Serilog.Events.LogEventLevel.Error
                : context.Request.Path.StartsWithSegments("/health")
                    ? Serilog.Events.LogEventLevel.Debug
                    : Serilog.Events.LogEventLevel.Information;
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Only fills in a body where there is none, so every response written by
    // a controller or by the exception middleware above passes through
    // untouched.
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Construction API v1");
        });
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCors("Default");

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapApplicationHealthChecks();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Construction API terminated unexpectedly");

    // Without this the process still exits 0, so an orchestrator sees a clean
    // shutdown and neither restarts the container nor raises an alert.
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Named so the tests can host this exact application.
/// </summary>
/// <remarks>
/// A file of top-level statements compiles to an internal <c>Program</c> class,
/// which <c>WebApplicationFactory&lt;T&gt;</c> cannot reach. Declaring it here
/// makes it public without changing a line of the startup above — and the
/// point of the HTTP tests is that they run the real pipeline, authentication
/// and authorization included, rather than a second copy of it assembled in a
/// test.
/// </remarks>
public partial class Program;
