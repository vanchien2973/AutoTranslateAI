using Domain.Enums;

namespace Application.Dtos;

public sealed record DownloadRequest(string Url, string OutputDirectory);
public sealed record DownloadResult(string FilePath, string? Title, double? DurationSeconds);
public sealed record DemucsRequest(
    string InputAudioPath,
    string OutputDirectory,
    string Model = "htdemucs",
    bool TwoStems = true);
public sealed record DemucsResult(string VocalsPath, string AccompanimentPath);
public sealed record AudioExtractionRequest(
    string InputVideoPath,
    string OutputAudioPath,
    int SampleRate = 16000,
    int Channels = 1);
public sealed record TimelineClip(string FilePath, double StartTimeSeconds);
public sealed record TimelineAssemblyRequest(IReadOnlyList<TimelineClip> Clips, string OutputPath);
public sealed record MixRequest(
    string VocalsPath,
    string BackgroundMusicPath,
    string OutputPath,
    double BgmGainDb = -12);
public sealed record RenderRequest(
    string VideoPath,
    string? AudioPath,
    string OutputPath,
    SubtitleMode SubtitleMode = SubtitleMode.None,
    string? SubtitlePath = null,
    string? LogoPath = null,
    LogoPosition LogoPosition = LogoPosition.BottomRight,
    double LogoScalePercent = 0.1,
    int LogoMargin = 16,
    string? SubtitleFontFamily = null,
    int SubtitleFontSize = 24,
    SubtitlePosition SubtitlePosition = SubtitlePosition.Bottom,
    bool SubtitleBold = false,
    bool SubtitleItalic = false);
