namespace Infrastructure.Configuration;

public sealed class MediaToolsOptions
{
    public const string SectionName = "MediaTools";

    public string YtDlpPath { get; init; } = "yt-dlp";
    public string FfmpegPath { get; init; } = "ffmpeg";
    public string DemucsPath { get; init; } = "demucs";
}
