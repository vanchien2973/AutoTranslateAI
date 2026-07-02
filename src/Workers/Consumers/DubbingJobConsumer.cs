using Application.Messaging;
using Application.Pipeline;
using MassTransit;

namespace Workers.Consumers;

public sealed class DubbingJobConsumer : IConsumer<DubbingJobRequested>
{
    private readonly PipelineRunner _runner;
    private readonly ILogger<DubbingJobConsumer> _logger;

    public DubbingJobConsumer(PipelineRunner runner, ILogger<DubbingJobConsumer> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DubbingJobRequested> context)
    {
        var message = context.Message;
        _logger.LogInformation("Job {JobId}: consuming dubbing request for {Url}", message.JobId, message.SourceUrl);

        var request = new PipelineRequest(
            message.JobId,
            message.SourceUrl,
            message.AudioLanguage,
            message.SubtitleLanguage,
            message.EnableDubbing);

        var result = await _runner.RunAsync(request, context.CancellationToken);

        _logger.LogInformation("Job {JobId}: done. Output URL: {Url}", message.JobId, result.OutputUrl);
    }
}
