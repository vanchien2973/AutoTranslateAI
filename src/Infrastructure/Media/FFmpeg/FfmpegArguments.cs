using System.Globalization;
using System.Text;
using Application.Dtos;

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

    public static IReadOnlyList<string> BuildRender(RenderRequest request) =>
    [
        "-y",
        "-i", request.VideoPath,
        "-i", request.AudioPath,
        "-map", "0:v:0",               // video from the source
        "-map", "1:a:0",               // audio from the new (dubbed/mixed) track
        "-c:v", "copy",                // keep video as-is (no re-encode)
        "-c:a", "aac",
        "-shortest",
        request.OutputPath,
    ];
}
