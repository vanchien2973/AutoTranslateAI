using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.FFmpeg;

public sealed class FfmpegVideoRenderer : IVideoRenderer
{
    private readonly MediaToolsOptions _tools;
    private readonly LogoOptions _logo;
    private readonly ILogger<FfmpegVideoRenderer> _logger;

    public FfmpegVideoRenderer(
        IOptions<MediaToolsOptions> tools,
        IOptions<LogoOptions> logo,
        ILogger<FfmpegVideoRenderer> logger)
    {
        _tools = tools.Value;
        _logo = logo.Value;
        _logger = logger;
    }

    public async Task<string> RenderAsync(RenderRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rendering {Video} + {Audio} into {Output}", request.VideoPath, request.AudioPath, request.OutputPath);

        var effective = LogoResolver.Resolve(request, _logo);
        if (effective.LogoPath is null && !string.IsNullOrWhiteSpace(_logo.Path))
        {
            _logger.LogWarning("Logo path '{Path}' not found; rendering without a watermark.", _logo.Path);
        }

        var arguments = FfmpegArguments.BuildRender(effective);
        await FfmpegRunner.RunAsync(_tools.FfmpegPath, arguments, request.OutputPath, cancellationToken);

        return request.OutputPath;
    }
}
