using Construction.API.Authorization;
using Construction.API.Extensions;
using Construction.API.Middleware;
using Construction.API.Services;
using Construction.Application;
using Construction.Application.Common.Interfaces;
using Construction.Infrastructure;
using Construction.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
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

    // The API runs behind a reverse proxy in every deployment, so the client
    // address must come from the forwarded headers. Without this, every
    // refresh-token audit row records the proxy instead of the caller.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Cleared because the proxy is not on a known address in a container
        // network; restrict these in an environment where it is.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

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
            .AllowAnyMethod());
    });

    var app = builder.Build();

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync();
    }

    // Must run before anything reads the client address or scheme.
    app.UseForwardedHeaders();

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
