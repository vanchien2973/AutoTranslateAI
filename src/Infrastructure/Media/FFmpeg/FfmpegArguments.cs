using System.Globalization;
using System.Text;
using Domain.Enums;

namespace Infrastructure.Media.FFmpeg;

internal static class FfmpegArguments
{
    public static IReadOnlyList<string> BuildExtractAudio(AudioExtractionRequest request) =>
    [
        "-y",                          // overwrite output if it already exists (resume re-runs)
        "-i", request.InputVideoPath,
        "-vn",                         // drop video, keep audio only
        "-acodec", "pcm_s16le",        // 16-bit PCM WAV (what whisper.net expects)
        "-ar", request.SampleRate.ToString(CultureInfo.InvariantCulture),
        "-ac", request.Channels.ToString(CultureInfo.InvariantCulture),
        request.OutputAudioPath,
    ];

    public static IReadOnlyList<string> BuildAssembleTimeline(TimelineAssemblyRequest request)
    {
        if (request.Clips.Count == 0)
        {
            throw new ArgumentException("At least one clip is required to assemble a timeline.", nameof(request));
        }

        var args = new List<string> { "-y" };
        foreach (var clip in request.Clips)
        {
            args.Add("-i");
            args.Add(clip.FilePath);
        }

        // Delay each clip to its start time, then mix them onto one track.
        var filter = new StringBuilder();
        for (var i = 0; i < request.Clips.Count; i++)
        {
            var delayMs = (long)Math.Round(request.Clips[i].StartTimeSeconds * 1000);
            filter.Append(CultureInfo.InvariantCulture, $"[{i}:a]adelay={delayMs}:all=1[d{i}];");
        }

        for (var i = 0; i < request.Clips.Count; i++)
        {
            filter.Append(CultureInfo.InvariantCulture, $"[d{i}]");
        }

        filter.Append(CultureInfo.InvariantCulture, $"amix=inputs={request.Clips.Count}:normalize=0[out]");

        args.Add("-filter_complex");
        args.Add(filter.ToString());
        args.Add("-map");
        args.Add("[out]");
        args.Add(request.OutputPath);
        return args;
    }

    public static IReadOnlyList<string> BuildMix(MixRequest request)
    {
        var gain = request.BgmGainDb.ToString("0.##", CultureInfo.InvariantCulture);
        var filter =
            $"[1:a]volume={gain}dB[bg];[0:a][bg]amix=inputs=2:duration=longest:normalize=0[out]";

        return
        [
            "-y",
            "-i", request.VocalsPath,
            "-i", request.BackgroundMusicPath,
            "-filter_complex", filter,
            "-map", "[out]",
            request.OutputPath,
        ];
    }

    public static IReadOnlyList<string> BuildRender(RenderRequest request)
    {
        var hasSubtitle = !string.IsNullOrWhiteSpace(request.SubtitlePath);
        var softsub = request.SubtitleMode == SubtitleMode.Softsub && hasSubtitle;
        var hardsub = request.SubtitleMode == SubtitleMode.Hardsub && hasSubtitle;
        var hasNewAudio = !string.IsNullOrWhiteSpace(request.AudioPath);
        var hasLogo = !string.IsNullOrWhiteSpace(request.LogoPath);

        var args = new List<string> { "-y", "-i", request.VideoPath };
        var nextInput = 1;

        var audioMap = "0:a:0?";                        // keep the source audio unless a new track is supplied
        if (hasNewAudio)
        {
            args.Add("-i");
            args.Add(request.AudioPath!);
            audioMap = $"{nextInput++}:a:0";
        }

        var subtitleInput = -1;
        if (softsub)
        {
            args.Add("-i");
            args.Add(request.SubtitlePath!);
            subtitleInput = nextInput++;
        }

        var logoInput = -1;
        if (hasLogo)
        {
            args.Add("-i");
            args.Add(request.LogoPath!);
            logoInput = nextInput++;
        }

        var videoFilter = BuildVideoFilter(request, hardsub, hasLogo, logoInput);

        if (videoFilter is not null)
        {
            args.Add("-filter_complex");
            args.Add(videoFilter);
            args.Add("-map");
            args.Add("[v]");
        }
        else
        {
            args.Add("-map");
            args.Add("0:v:0");
        }

        args.Add("-map");
        args.Add(audioMap);
        if (softsub)
        {
            args.Add("-map");
            args.Add($"{subtitleInput}:s:0");
        }

        if (videoFilter is null)
        {
            args.Add("-c:v");
            args.Add("copy");
        }

        args.Add("-c:a");
        args.Add(hasNewAudio ? "aac" : "copy");
        if (softsub)
        {
            args.Add("-c:s");
            args.Add("mov_text");
        }

        args.Add("-shortest");
        args.Add(request.OutputPath);
        return args;
    }

    private static string? BuildVideoFilter(RenderRequest request, bool hardsub, bool hasLogo, int logoInput)
    {
        if (!hardsub && !hasLogo)
        {
            return null;
        }

        var filter = new StringBuilder();

        if (hasLogo)
        {
            var scale = request.LogoScalePercent.ToString("0.###", CultureInfo.InvariantCulture);
            // Scale the logo to a fraction of the video height (aspect-preserving), then overlay at the chosen corner.
            filter.Append(CultureInfo.InvariantCulture,
                $"[{logoInput}:v][0:v]scale2ref=w=oh*mdar:h=ih*{scale}[logo][base];[base][logo]overlay={OverlayXy(request.LogoPosition, request.LogoMargin)}");
        }
        else
        {
            filter.Append("[0:v]");
        }

        if (hardsub)
        {
            filter.Append(CultureInfo.InvariantCulture,
                $"{(hasLogo ? "," : string.Empty)}subtitles={EscapeSubtitlePath(request.SubtitlePath!)}:force_style='{BuildForceStyle(request)}'");
        }

        filter.Append("[v]");
        return filter.ToString();
    }

    private static string BuildForceStyle(RenderRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.SubtitleFontFamily))
        {
            parts.Add($"FontName={request.SubtitleFontFamily}");
        }

        parts.Add($"FontSize={request.SubtitleFontSize.ToString(CultureInfo.InvariantCulture)}");
        parts.Add($"Bold={(request.SubtitleBold ? -1 : 0)}");
        parts.Add($"Italic={(request.SubtitleItalic ? -1 : 0)}");
        parts.Add($"Alignment={SubtitleAlignment(request.SubtitlePosition)}");
        return string.Join(",", parts);
    }

    private static int SubtitleAlignment(SubtitlePosition position) => position switch
    {
        SubtitlePosition.Top => 8,
        SubtitlePosition.Middle => 5,
        _ => 2,
    };

    private static string OverlayXy(LogoPosition position, int margin) => position switch
    {
        LogoPosition.TopLeft => $"{margin}:{margin}",
        LogoPosition.TopRight => $"W-w-{margin}:{margin}",
        LogoPosition.BottomLeft => $"{margin}:H-h-{margin}",
        _ => $"W-w-{margin}:H-h-{margin}",   // BottomRight
    };

    private static string EscapeSubtitlePath(string path)
    {
        var normalized = path.Replace('\\', '/').Replace(":", "\\:");
        return $"'{normalized}'";
    }
}
