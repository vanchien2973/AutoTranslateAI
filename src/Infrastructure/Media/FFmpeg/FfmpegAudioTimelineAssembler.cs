using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.FFmpeg;

public sealed class FfmpegAudioTimelineAssembler : IAudioTimelineAssembler
{
    private readonly MediaToolsOptions _tools;
    private readonly ILogger<FfmpegAudioTimelineAssembler> _logger;

    public FfmpegAudioTimelineAssembler(IOptions<MediaToolsOptions> tools, ILogger<FfmpegAudioTimelineAssembler> logger)
    {
        _tools = tools.Value;
        _logger = logger;
    }

    public async Task<string> AssembleAsync(TimelineAssemblyRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assembling {Count} TTS clips into {Output}", request.Clips.Count, request.OutputPath);

        var arguments = FfmpegArguments.BuildAssembleTimeline(request);
        await FfmpegRunner.RunAsync(_tools.FfmpegPath, arguments, request.OutputPath, cancellationToken);

        return request.OutputPath;
    }
}
