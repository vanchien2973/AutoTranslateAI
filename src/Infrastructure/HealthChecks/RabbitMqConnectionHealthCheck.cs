using System.Net.Sockets;
using Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Infrastructure.HealthChecks;

/// <summary>
/// Phase-0 readiness check: confirms the RabbitMQ broker is reachable by opening a TCP connection
/// to Host:Port. Replace with MassTransit's built-in bus health check once messaging is wired.
/// </summary>
public sealed class RabbitMqConnectionHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqConnectionHealthCheck(IOptions<RabbitMqOptions> options) => _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.Host, _options.Port, cancellationToken);
            return client.Connected
                ? HealthCheckResult.Healthy($"TCP {_options.Host}:{_options.Port} reachable")
                : HealthCheckResult.Unhealthy($"Could not connect to {_options.Host}:{_options.Port}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"RabbitMQ {_options.Host}:{_options.Port} not reachable", ex);
        }
    }
}
