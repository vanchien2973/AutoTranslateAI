using Application;
using Application.Interfaces;
using Infrastructure;
using Serilog;
using Workers.Consumers;
using Workers.Steps;

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
    // Register the MassTransit consumer from this assembly so the Worker actually processes jobs.
    // Passing one marker registers every consumer in the Workers assembly (Phase1Consumer + Phase2Consumer).
    builder.Services.AddInfrastructure(builder.Configuration, typeof(Phase1Consumer));

    // Pipeline steps, registered in execution order; PipelineRunner resolves IEnumerable<IPipelineStep>
    // and runs them ordered by StepType (Download..Upload) in a single pass for this milestone.
    builder.Services.AddTransient<IPipelineStep, DownloadStep>();
    builder.Services.AddTransient<IPipelineStep, ExtractAudioStep>();
    builder.Services.AddTransient<IPipelineStep, SeparateBgmStep>();
    builder.Services.AddTransient<IPipelineStep, TranscribeStep>();
    builder.Services.AddTransient<IPipelineStep, TranslateStep>();
    builder.Services.AddTransient<IPipelineStep, TtsStep>();
    builder.Services.AddTransient<IPipelineStep, GenSubtitleStep>();
    builder.Services.AddTransient<IPipelineStep, MixStep>();
    builder.Services.AddTransient<IPipelineStep, RenderStep>();
    builder.Services.AddTransient<IPipelineStep, UploadStep>();
    builder.Services.AddScoped<PipelineRunner>();
    builder.Services.AddScoped<IPublishStep, PublishStep>();
    builder.Services.AddHostedService<Workers.Services.CleanupBackgroundService>();

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
