using Application.Dtos;
using Application.Interfaces;
using CliWrap;
using CliWrap.Buffered;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.Downloader;

public sealed class YtDlpVideoDownloader : IVideoDownloader
{
    private readonly MediaToolsOptions _tools;
    private readonly ILogger<YtDlpVideoDownloader> _logger;

    public YtDlpVideoDownloader(IOptions<MediaToolsOptions> tools, ILogger<YtDlpVideoDownloader> logger)
    {
        _tools = tools.Value;
        _logger = logger;
    }

    public async Task<DownloadResult> DownloadAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var arguments = YtDlpArguments.BuildDownload(request.Url, request.OutputDirectory);
        _logger.LogInformation("Downloading {Url} with yt-dlp", request.Url);

        var result = await Cli.Wrap(_tools.YtDlpPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"yt-dlp failed (exit {result.ExitCode}) for '{request.Url}': {result.StandardError}");
        }

        var filePath = YtDlpOutputParser.ParseFinalPath(result.StandardOutput)
            ?? throw new InvalidOperationException(
                $"yt-dlp did not report an output file for '{request.Url}'.");

        return new DownloadResult(filePath, Title: null, DurationSeconds: null);
    }
}
