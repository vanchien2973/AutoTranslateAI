using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.FFmpeg;

public sealed class FfmpegVideoRenderer : IVideoRenderer
{
    private readonly MediaToolsOptions _tools;
    private readonly ILogger<FfmpegVideoRenderer> _logger;

    public FfmpegVideoRenderer(IOptions<MediaToolsOptions> tools, ILogger<FfmpegVideoRenderer> logger)
    {
        _tools = tools.Value;
        _logger = logger;
    }

    public async Task<string> RenderAsync(RenderRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rendering {Video} + {Audio} into {Output}", request.VideoPath, request.AudioPath, request.OutputPath);

        var arguments = FfmpegArguments.BuildRender(request);
        await FfmpegRunner.RunAsync(_tools.FfmpegPath, arguments, request.OutputPath, cancellationToken);

        return request.OutputPath;
    }
}
