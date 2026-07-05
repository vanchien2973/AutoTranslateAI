using Api.Consumers;
using Api.Extensions;
using Api.Hubs;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
    builder.Services.AddControllers();
    builder.Services.AddSignalR();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, typeof(JobProgressConsumer));
    builder.Services.AddSwagger();

    // Liveness has no checks (process is up); readiness runs the "ready"-tagged checks
    // (Postgres now, RabbitMQ/R2 as they are wired in Infrastructure).
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseCors(CorsPolicy);
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<JobProgressHub>("/hubs/jobs");
    app.ConfigureSwagger();
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthJson,
    });

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
