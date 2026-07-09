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

        var effective = request;
        if (!string.IsNullOrWhiteSpace(_logo.Path))
        {
            if (IsRemote(_logo.Path) || File.Exists(_logo.Path))
            {
                effective = request with
                {
                    LogoPath = _logo.Path,
                    LogoPosition = _logo.Position,
                    LogoScalePercent = _logo.ScalePercent,
                    LogoMargin = _logo.Margin,
                };
            }
            else
            {
                _logger.LogWarning("Logo path '{Path}' not found; rendering without a watermark.", _logo.Path);
            }
        }

        var arguments = FfmpegArguments.BuildRender(effective);
        await FfmpegRunner.RunAsync(_tools.FfmpegPath, arguments, request.OutputPath, cancellationToken);

        return request.OutputPath;
    }

    private static bool IsRemote(string path) =>
        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
