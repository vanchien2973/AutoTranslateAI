using Api.Hubs;
using Application.Messaging;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Api.Consumers;

public sealed class JobProgressConsumer : IConsumer<JobProgressUpdated>
{
    private readonly IHubContext<JobProgressHub> _hub;

    public JobProgressConsumer(IHubContext<JobProgressHub> hub) => _hub = hub;

    public Task Consume(ConsumeContext<JobProgressUpdated> context)
    {
        var message = context.Message;
        var progress = new JobProgress(message.JobId, message.Status, message.CurrentStep, message.ProgressPercent);

        return _hub.Clients
            .Group(JobProgressHub.GroupFor(message.JobId))
            .SendAsync("ReceiveProgress", progress, context.CancellationToken);
    }
}
