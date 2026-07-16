using Api.Hubs;
using Application.Messaging;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Api.Consumers;

public sealed class JobMetricsConsumer : IConsumer<JobMetricsUpdated>
{
    private readonly IHubContext<JobProgressHub> _hub;

    public JobMetricsConsumer(IHubContext<JobProgressHub> hub) => _hub = hub;

    public Task Consume(ConsumeContext<JobMetricsUpdated> context)
    {
        var message = context.Message;
        var metrics = new JobMetrics(message.JobId, message.CpuPercent, message.MemoryUsedBytes, message.MemoryTotalBytes);

        return _hub.Clients
            .Group(JobProgressHub.GroupFor(message.JobId))
            .SendAsync("ReceiveMetrics", metrics, context.CancellationToken);
    }
}
