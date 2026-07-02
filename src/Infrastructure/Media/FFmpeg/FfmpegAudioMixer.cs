using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.FFmpeg;

public sealed class FfmpegAudioMixer : IAudioMixer
{
    private readonly MediaToolsOptions _tools;
    private readonly ILogger<FfmpegAudioMixer> _logger;

    public FfmpegAudioMixer(IOptions<MediaToolsOptions> tools, ILogger<FfmpegAudioMixer> logger)
    {
        _tools = tools.Value;
        _logger = logger;
    }

    public async Task<string> MixAsync(MixRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mixing vocals + bgm (gain {Gain}dB) into {Output}", request.BgmGainDb, request.OutputPath);

        var arguments = FfmpegArguments.BuildMix(request);
        await FfmpegRunner.RunAsync(_tools.FfmpegPath, arguments, request.OutputPath, cancellationToken);

        return request.OutputPath;
    }
}
