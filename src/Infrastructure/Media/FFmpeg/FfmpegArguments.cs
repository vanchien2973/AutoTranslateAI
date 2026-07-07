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

        var args = new List<string> { "-y", "-i", request.VideoPath };

        if (hasNewAudio)
        {
            args.Add("-i");
            args.Add(request.AudioPath!);
        }

        if (softsub)
        {
            args.Add("-i");
            args.Add(request.SubtitlePath!);
        }

        args.Add("-map");
        args.Add("0:v:0");             // video from the source
        args.Add("-map");
        args.Add(hasNewAudio ? "1:a:0" : "0:a:0?");   // new dubbed/mixed track, else keep the source audio
        if (softsub)
        {
            // The subtitle is the last input: index 2 when there's a new audio track, otherwise 1.
            args.Add("-map");
            args.Add(hasNewAudio ? "2:s:0" : "1:s:0");
        }

        if (hardsub)
        {
            // Burn subtitles into the picture; this re-encodes the video stream.
            args.Add("-vf");
            args.Add($"subtitles={EscapeSubtitlePath(request.SubtitlePath!)}");
        }
        else
        {
            args.Add("-c:v");
            args.Add("copy");          // keep video as-is (no re-encode)
        }

        args.Add("-c:a");
        args.Add(hasNewAudio ? "aac" : "copy");   // re-encode the dubbed track; copy the untouched source audio
        if (softsub)
        {
            args.Add("-c:s");
            args.Add("mov_text");      // MP4-compatible soft subtitle codec
        }

        args.Add("-shortest");
        args.Add(request.OutputPath);
        return args;
    }

    private static string EscapeSubtitlePath(string path)
    {
        var normalized = path.Replace('\\', '/').Replace(":", "\\:");
        return $"'{normalized}'";
    }
}
