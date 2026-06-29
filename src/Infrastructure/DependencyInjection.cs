using Infrastructure.Configuration;
using Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddProviders(configuration);
        services.AddMessaging(configuration);
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        // TODO: services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

        // DB readiness check (tag "ready"); the Api maps it at /health/ready.
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);
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

        var providers = configuration.GetSection(ProviderOptions.SectionName).Get<ProviderOptions>() ?? new ProviderOptions();
        if (string.Equals(providers.Storage, "R2", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHealthChecks()
                .AddCheck<R2StorageHealthCheck>("r2", tags: ["ready"]);
        }

        // TODO: switch on configuration["Providers:Tts"] etc. to register the chosen ITtsService /
        // ISpeechToTextService / ITranslationService / IStorageService implementation.
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<RabbitMqOptions>(configuration, RabbitMqOptions.SectionName);
        services.AddHealthChecks()
            .AddCheck<RabbitMqConnectionHealthCheck>("rabbitmq", tags: ["ready"]);

        // TODO: services.AddMassTransit(x => { x.UsingRabbitMq(...); });
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
