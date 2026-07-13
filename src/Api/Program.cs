using Api.Consumers;
using Api.Extensions;
using Api.Filters;
using Api.Hubs;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AutoTranslateAI API");

    var builder = WebApplication.CreateBuilder(args);

    // Replace the default logging with Serilog, configured from the "Serilog" section.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // CORS so a frontend on another origin can call the API + SignalR hub. SignalR needs AllowCredentials,
    // which forbids AllowAnyOrigin — so origins are an explicit allow-list (configurable via "Cors:AllowedOrigins").
    const string CorsPolicy = "frontend";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000", "http://localhost:5173"];

    // Add services to the container.
    builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
    builder.Services.AddControllers(options => options.Filters.Add<ValidationExceptionFilter>());
    builder.Services.AddSignalR();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, typeof(JobProgressConsumer));
    builder.Services.AddSwagger();

    // Basic API-key auth for shared local instances (opt-in via Auth:Enabled). Disabled => everything anonymous.
    builder.Services.AddApiKeyAuth(builder.Configuration);

    // Liveness has no checks (process is up); readiness runs the "ready"-tagged checks
    // (Postgres now, RabbitMQ/R2 as they are wired in Infrastructure).
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Apply EF migrations at startup when opted in (Database:MigrateOnStartup) so `docker compose up`
    // works end-to-end. Off by default for local dev (run `dotnet ef database update` yourself).
    if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetService<Infrastructure.Persistence.AppDbContext>();
        if (db is not null)
        {
            db.Database.Migrate();
            Log.Information("Applied database migrations on startup");
        }
    }

    // Configure the HTTP request pipeline.
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseCors(CorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<JobProgressHub>("/hubs/jobs");
    app.ConfigureSwagger();
    // Health probes stay public even when API-key auth is enforced (for Docker healthchecks).
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthJson,
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AutoTranslateAI API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Writes the readiness report as JSON so a single GET /health/ready shows each dependency's status.
static Task WriteHealthJson(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            error = e.Value.Exception?.Message,
        }),
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
