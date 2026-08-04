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

    builder.Services.AddJwtBearerAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();
    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddAuthRateLimiting();

    builder.Services.AddTrustedProxyForwarding(builder.Configuration);

    // The product's recurring job. Safe to run on every replica — each sweep
    // claims a row before notifying, so nobody is told twice.
    builder.Services.AddHostedService<DailyReminderService>();

    builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Default", policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
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

    app.UseSecurityHeaders();

    // Request logging wraps the exception handler, not the other way round.
    // Inside it, Serilog would see every exception before it was translated
    // and record a 500 — so a duplicate employee number the client correctly
    // received as 409, or a page the user navigated away from, would land in
    // the log as a server fault and drive false alerts.
    app.UseSerilogRequestLogging();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

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
    app.MapHealthChecks("/health");

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
