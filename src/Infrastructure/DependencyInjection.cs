using Application.Interfaces;
using Infrastructure.AI.Review;
using Infrastructure.AI.SpeechToText;
using Infrastructure.AI.TextToSpeech;
using Infrastructure.AI.Translation;
using Infrastructure.Review;
using Infrastructure.Configuration;
using Infrastructure.HealthChecks;
using Infrastructure.Messaging;
using Infrastructure.Monitoring;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Media.Demucs;
using Infrastructure.Media.Downloader;
using Infrastructure.Media.FFmpeg;
using Infrastructure.Publishing;
using Infrastructure.Resilience;
using Infrastructure.Storage;
using Infrastructure.Usage;
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
        services.AddPublishing();
        services.AddMediaTools(configuration);
        services.AddMessaging(configuration, consumerMarkers);
        return services;
    }

    private static IServiceCollection AddPublishing(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton<IPublisher, YouTubePublisher>();
        services.AddSingleton<IPublisher, FacebookPublisher>();
        services.AddSingleton<IPublisher, TikTokPublisher>();
        services.AddSingleton<IPublisherFactory, PublisherFactory>();

        services.AddSingleton<IOAuthProvider, YouTubeOAuthProvider>();
        services.AddSingleton<IOAuthProvider, FacebookOAuthProvider>();
        services.AddSingleton<IOAuthProvider, TikTokOAuthProvider>();
        services.AddSingleton<IOAuthProviderFactory, OAuthProviderFactory>();

        return services;
    }

    private static IServiceCollection AddMediaTools(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<MediaToolsOptions>(configuration, MediaToolsOptions.SectionName);
        services.AddValidatedOptions<WorkspaceOptions>(configuration, WorkspaceOptions.SectionName);
        services.AddValidatedOptions<LogoOptions>(configuration, LogoOptions.SectionName);
        services.AddValidatedOptions<CleanupOptions>(configuration, CleanupOptions.SectionName);
        services.AddSingleton<IWorkspaceManager, WorkspaceManager>();
        services.AddSingleton<IWorkspaceJanitor, WorkspaceJanitor>();
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
            // Factory gives short-lived contexts (JobStepTracker uses its own so it never shares the
            // change-tracker with the aggregate); the scoped AppDbContext (from the factory) is for the repository.
            services.AddDbContextFactory<AppDbContext>(options => options
                .UseNpgsql(connectionString));
            services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            services.AddScoped<IDubbingJobRepository, DubbingJobRepository>();

            // Auto-publish
            services.AddScoped<IPlatformCredentialRepository, PlatformCredentialRepository>();
            services.AddScoped<IChannelConnectionRepository, ChannelConnectionRepository>();
            services.AddScoped<IPublishResultRepository, PublishResultRepository>();

            // Persist per-step status to JobSteps so a retried message resumes from the failed step.
            services.AddScoped<IJobStepTracker, JobStepTracker>();

            // Cost/quota tracking: tracker is a singleton (injected into singleton providers) and uses the context factory; the read side is a scoped repository for the /api/usage query.
            services.AddSingleton<IUsageTracker, UsageTracker>();
            services.AddScoped<IUsageRepository, UsageRepository>();

            // DB readiness check (tag "ready"); the Api maps it at /health/ready.
            services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);
        }
        else
        {
            // No DB configured: skip resume tracking (every run starts fresh).
            services.AddScoped<IJobStepTracker, NullJobStepTracker>();
            services.AddSingleton<IUsageTracker, NullUsageTracker>();
        }

        return services;
    }

    private static IServiceCollection AddProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<ProviderOptions>(configuration, ProviderOptions.SectionName);
        services.AddSingleton<IProviderRegistry, ConfiguredProviderRegistry>();
        services.AddValidatedOptions<AzureSpeechOptions>(configuration, AzureSpeechOptions.SectionName);
        services.AddValidatedOptions<OpenAIOptions>(configuration, OpenAIOptions.SectionName);
        services.AddValidatedOptions<OllamaOptions>(configuration, OllamaOptions.SectionName);
        services.AddValidatedOptions<WhisperNetOptions>(configuration, WhisperNetOptions.SectionName);
        services.AddValidatedOptions<PiperOptions>(configuration, PiperOptions.SectionName);
        services.AddValidatedOptions<R2Options>(configuration, R2Options.SectionName);
        services.AddValidatedOptions<ResilienceOptions>(configuration, ResilienceOptions.SectionName);
        services.AddValidatedOptions<PricingOptions>(configuration, PricingOptions.SectionName);

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

        // Synchronous chat completion (review assistant) — shares the Translation provider selection.
        services.AddSingleton<ILlmCompletionService>(sp => providers.Translation switch
        {
            "OpenAI" => ActivatorUtilities.CreateInstance<OpenAiChatCompletionService>(sp),
            var other => throw new InvalidOperationException($"Unknown Translation provider: '{other}'"),
        });

        // Review chat history + pending proposals live in memory (single local instance).
        services.AddSingleton<IReviewSessionStore, InMemoryReviewSessionStore>();

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

                // Serialize processing so the same job is never handled by two consumers at once.
                cfg.UseConcurrencyLimit(rabbit.ConcurrencyLimit);

                cfg.ConfigureEndpoints(context);
            });
        });

        // Progress emission (Worker publishes JobProgressUpdated; API consumes → SignalR).
        services.AddScoped<IProgressNotifier, MassTransitProgressNotifier>();

        // Resource metrics: worker samples /proc while a job runs and publishes JobMetricsUpdated; API relays.
        services.AddSingleton<ISystemMetricsSampler, ProcSystemMetricsSampler>();
        services.AddScoped<IMetricsNotifier, MassTransitMetricsNotifier>();
        services.AddScoped<IJobMetricsMonitor, JobMetricsMonitor>();

        // Integration-event publishing behind an Application interface (keeps MassTransit out of handlers).
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

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
