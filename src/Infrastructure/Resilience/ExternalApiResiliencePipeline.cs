using System.ClientModel;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Infrastructure.Resilience;

public sealed class ExternalApiResiliencePipeline
{
    public ResiliencePipeline Pipeline { get; }

    public ExternalApiResiliencePipeline(IOptions<ResilienceOptions> options)
    {
        var config = options.Value;

        Pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // Retry only transient faults: network, per-attempt timeouts, and OpenAI 408/429/5xx.
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<IOException>()
                    .Handle<ClientResultException>(IsTransientStatus),
                MaxRetryAttempts = config.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(config.BaseDelaySeconds),
            })
            // Inner strategy: caps each attempt so a hung call is aborted and retried instead of blocking the worker.
            .AddTimeout(TimeSpan.FromSeconds(config.PerAttemptTimeoutSeconds))
            .Build();
    }

    private static bool IsTransientStatus(ClientResultException exception) =>
        exception.Status is 408 or 429 or >= 500;
}
