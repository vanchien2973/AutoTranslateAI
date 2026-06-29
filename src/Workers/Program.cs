using Application;
using Infrastructure;
using Serilog;
using Workers;

// Bootstrap logger: captures anything thrown before the host (and full Serilog config) is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AutoTranslateAI Workers");

    var builder = Host.CreateApplicationBuilder(args);

    // HostApplicationBuilder has no .Host; register Serilog as the logging provider on the services.
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AutoTranslateAI Workers terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
