using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.FFmpeg;

public sealed class FfmpegAudioExtractor : IAudioExtractor
{
    private readonly MediaToolsOptions _tools;
    private readonly ILogger<FfmpegAudioExtractor> _logger;

    public FfmpegAudioExtractor(IOptions<MediaToolsOptions> tools, ILogger<FfmpegAudioExtractor> logger)
    {
        _tools = tools.Value;
        _logger = logger;
    }

    public async Task<string> ExtractAudioAsync(AudioExtractionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extracting audio from {Input} to {Output}", request.InputVideoPath, request.OutputAudioPath);

        var arguments = FfmpegArguments.BuildExtractAudio(request);
        await FfmpegRunner.RunAsync(_tools.FfmpegPath, arguments, request.OutputAudioPath, cancellationToken);

        return request.OutputAudioPath;
    }
}
