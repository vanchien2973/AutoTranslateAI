using Application.Interfaces;
using Infrastructure.AI.SpeechToText;
using Infrastructure.AI.TextToSpeech;
using Infrastructure.AI.Translation;
using Infrastructure.Configuration;
using Infrastructure.HealthChecks;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Media.Demucs;
using Infrastructure.Media.Downloader;
using Infrastructure.Media.FFmpeg;
using Infrastructure.Resilience;
using Infrastructure.Storage;
using Infrastructure.Workspace;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Repositories;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        params Type[] consumerMarkers)
    {
        services.AddPersistence(configuration);
        services.AddProviders(configuration);
        services.AddMediaTools(configuration);
        services.AddMessaging(configuration, consumerMarkers);
        return services;
    }

    private static IServiceCollection AddMediaTools(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<MediaToolsOptions>(configuration, MediaToolsOptions.SectionName);
        services.AddValidatedOptions<WorkspaceOptions>(configuration, WorkspaceOptions.SectionName);
        services.AddSingleton<IWorkspaceManager, WorkspaceManager>();
        services.AddSingleton<IVideoDownloader, YtDlpVideoDownloader>();
        services.AddSingleton<IAudioExtractor, FfmpegAudioExtractor>();
        services.AddSingleton<IDemucsService, DemucsService>();
        services.AddSingleton<IAudioTimelineAssembler, FfmpegAudioTimelineAssembler>();
        services.AddSingleton<IAudioMixer, FfmpegAudioMixer>();
        services.AddSingleton<IVideoRenderer, FfmpegVideoRenderer>();
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        // Pipeline state snapshot lives on the local workspace volume, so it works with or without a DB.
        services.AddScoped<IPipelineStateStore, FilePipelineStateStore>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IDubbingJobRepository, DubbingJobRepository>();

            // Persist per-step status to JobSteps so a retried message resumes from the failed step.
            services.AddScoped<IJobStepTracker, JobStepTracker>();

            // DB readiness check (tag "ready"); the Api maps it at /health/ready.
            services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);
        }
        else
        {
            // No DB configured: skip resume tracking (every run starts fresh).
            services.AddScoped<IJobStepTracker, NullJobStepTracker>();
        }

        return services;
    }

    private static IServiceCollection AddProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<ProviderOptions>(configuration, ProviderOptions.SectionName);
        services.AddValidatedOptions<AzureSpeechOptions>(configuration, AzureSpeechOptions.SectionName);
        services.AddValidatedOptions<OpenAIOptions>(configuration, OpenAIOptions.SectionName);
        services.AddValidatedOptions<OllamaOptions>(configuration, OllamaOptions.SectionName);
        services.AddValidatedOptions<WhisperNetOptions>(configuration, WhisperNetOptions.SectionName);
        services.AddValidatedOptions<PiperOptions>(configuration, PiperOptions.SectionName);
        services.AddValidatedOptions<R2Options>(configuration, R2Options.SectionName);
        services.AddValidatedOptions<ResilienceOptions>(configuration, ResilienceOptions.SectionName);

        // Shared Polly pipeline (retry + per-attempt timeout) injected into the OpenAI/Azure adapters.
        services.AddSingleton<ExternalApiResiliencePipeline>();

        var providers = configuration.GetSection(ProviderOptions.SectionName).Get<ProviderOptions>() ?? new ProviderOptions();
        if (string.Equals(providers.Storage, "R2", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHealthChecks()
                .AddCheck<R2StorageHealthCheck>("r2", tags: ["ready"]);
        }

        services.AddSingleton<ISpeechToTextService>(sp => providers.SpeechToText switch
        {
            "WhisperNet" => ActivatorUtilities.CreateInstance<WhisperNetSpeechToTextService>(sp),
            var other => throw new InvalidOperationException($"Unknown SpeechToText provider: '{other}'"),
        });

        // Translation provider
        services.AddSingleton<ITranslationService>(sp => providers.Translation switch
        {
            "OpenAI" => ActivatorUtilities.CreateInstance<OpenAiTranslationService>(sp),
            var other => throw new InvalidOperationException($"Unknown Translation provider: '{other}'"),
        });

        // Text-to-speech provider.
        services.AddSingleton<ITtsService>(sp => providers.Tts switch
        {
            "Azure" => ActivatorUtilities.CreateInstance<AzureTtsService>(sp),
            var other => throw new InvalidOperationException($"Unknown Tts provider: '{other}'"),
        });

        // Output storage provider.
        services.AddSingleton<IStorageService>(sp => providers.Storage switch
        {
            "R2" => ActivatorUtilities.CreateInstance<R2StorageService>(sp),
            var other => throw new InvalidOperationException($"Unknown Storage provider: '{other}'"),
        });

        // TODO: add Ollama (Translation), Piper (Tts), Local/AzureBlob (Storage) cases when needed.
        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Type[] consumerMarkers)
    {
        services.AddValidatedOptions<RabbitMqOptions>(configuration, RabbitMqOptions.SectionName);
        services.AddHealthChecks()
            .AddCheck<RabbitMqConnectionHealthCheck>("rabbitmq", tags: ["ready"]);

        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            // Only the Worker passes consumer markers; the Api registers the bus for publishing only.
            if (consumerMarkers.Length > 0)
            {
                x.AddConsumers(consumerMarkers.Select(marker => marker.Assembly).Distinct().ToArray());
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, rabbit.Port, rabbit.VirtualHost, host =>
                {
                    host.Username(rabbit.Username);
                    host.Password(rabbit.Password);
                });

                // Broker-level retry: on an unhandled consumer exception the message is redelivered with an
                // incremental back-off. The pipeline is resume-aware (skips Completed JobSteps), so a retry
                // re-runs only from the step that failed instead of the whole job.
                cfg.UseMessageRetry(r => r.Incremental(
                    rabbit.RetryLimit,
                    TimeSpan.FromSeconds(rabbit.RetryInitialIntervalSeconds),
                    TimeSpan.FromSeconds(rabbit.RetryIntervalIncrementSeconds)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IServiceCollection AddValidatedOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where T : class
    {
        services.AddOptions<T>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations();
        return services;
    }
}
