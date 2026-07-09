using Application.Dtos;
using Application.Enums;
using Domain.Enums;
using Infrastructure.Media.FFmpeg;

namespace Infrastructure.Tests.Media;

public class FfmpegRenderArgumentsTests
{
    [Fact]
    public void Given_NoSubtitle_When_BuildRender_Then_CopiesVideoWithoutSubtitles()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", "a.wav", "o.mp4"));

        // Assert
        args.Should().ContainInOrder("-c:v", "copy");
        args.Should().NotContain("-vf");
        args.Should().NotContain("-c:s");
    }

    [Fact]
    public void Given_Softsub_When_BuildRender_Then_MuxesToggleableSubtitleTrack()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", "a.wav", "o.mp4", SubtitleMode.Softsub, "sub.srt"));

        // Assert
        args.Should().Contain("sub.srt");             // added as a third input
        args.Should().ContainInOrder("-map", "2:s:0");
        args.Should().ContainInOrder("-c:s", "mov_text");
        args.Should().ContainInOrder("-c:v", "copy"); // no re-encode for softsub
        args.Should().NotContain("-vf");
    }

    [Fact]
    public void Given_Hardsub_When_BuildRender_Then_BurnsSubtitlesAndReencodesVideo()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", "a.wav", "o.mp4", SubtitleMode.Hardsub, "sub.srt"));

        // Assert — hardsub burns via the filtergraph
        args.Should().Contain("-filter_complex");
        Filter(args).Should().Contain("subtitles=");
        args.Should().ContainInOrder("-map", "[v]");
        args.Should().NotContain("copy");   // video is re-encoded, not copied
        args.Should().NotContain("-c:s");   // not muxed as a separate track
    }

    [Fact]
    public void Given_HardsubModeButNoPath_When_BuildRender_Then_FallsBackToPlainRender()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", "a.wav", "o.mp4", SubtitleMode.Hardsub, SubtitlePath: null));

        // Assert
        args.Should().ContainInOrder("-c:v", "copy");
        args.Should().NotContain("-vf");
    }

    [Fact]
    public void Given_WindowsSubtitlePath_When_BuildRender_Hardsub_Then_EscapesForFilterGraph()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", "a.wav", "o.mp4", SubtitleMode.Hardsub, @"C:\app\sub.srt"));

        // Assert — backslashes normalised, colon escaped, wrapped in quotes (inside the filtergraph)
        Filter(args).Should().Contain(@"subtitles='C\:/app/sub.srt'");
    }

    [Fact]
    public void Given_NoAudioTrack_When_BuildRender_Then_KeepsSourceAudio()
    {
        // Act — subtitle-only / no-dub render (AudioPath null)
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", AudioPath: null, "o.mp4"));

        // Assert
        args.Should().ContainInOrder("-map", "0:a:0?");   // keep the source video's own audio
        args.Should().ContainInOrder("-c:a", "copy");
        args.Should().NotContain("1:a:0");
    }

    [Fact]
    public void Given_NoAudioTrack_Softsub_When_BuildRender_Then_SubtitleIsSecondInput()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", null, "o.mp4", SubtitleMode.Softsub, "sub.srt"));

        // Assert — with no audio input, the srt is input index 1
        args.Should().ContainInOrder("-map", "0:a:0?");
        args.Should().ContainInOrder("-map", "1:s:0");
        args.Should().ContainInOrder("-c:s", "mov_text");
    }

    [Fact]
    public void Given_NoAudioTrack_Hardsub_When_BuildRender_Then_BurnsSubsAndCopiesSourceAudio()
    {
        // Act
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", null, "o.mp4", SubtitleMode.Hardsub, "sub.srt"));

        // Assert
        args.Should().Contain("-filter_complex");
        Filter(args).Should().Contain("subtitles=");
        args.Should().NotContain("-c:v");             // video re-encoded, not copied
        args.Should().ContainInOrder("-c:a", "copy"); // source audio kept as-is
    }

    private static string Filter(IReadOnlyList<string> args)
    {
        var list = args.ToList();
        return list[list.IndexOf("-filter_complex") + 1];
    }

    [Fact]
    public void Given_Logo_When_BuildRender_Then_OverlaysViaFilterComplexAndReencodes()
    {
        // Act — logo is the third input (video, audio, logo)
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", "a.wav", "o.mp4", LogoPath: "logo.png"));

        // Assert
        args.Should().Contain("-filter_complex");
        Filter(args).Should().Contain("[2:v]").And.Contain("scale2ref").And.Contain("overlay=");
        args.Should().ContainInOrder("-map", "[v]");
        args.Should().NotContain("-c:v");   // filtergraph re-encodes, not copied
    }

    [Fact]
    public void Given_LogoBottomRight_When_BuildRender_Then_OverlayAtBottomRightCorner()
    {
        var args = FfmpegArguments.BuildRender(new RenderRequest(
            "v.mp4", "a.wav", "o.mp4", LogoPath: "logo.png", LogoPosition: LogoPosition.BottomRight, LogoMargin: 16));

        Filter(args).Should().Contain("overlay=W-w-16:H-h-16");
    }

    [Fact]
    public void Given_LogoTopLeft_When_BuildRender_Then_OverlayAtTopLeftCorner()
    {
        var args = FfmpegArguments.BuildRender(new RenderRequest(
            "v.mp4", "a.wav", "o.mp4", LogoPath: "logo.png", LogoPosition: LogoPosition.TopLeft, LogoMargin: 20));

        Filter(args).Should().Contain("overlay=20:20");
    }

    [Fact]
    public void Given_LogoAndHardsub_When_BuildRender_Then_OverlaysThenBurnsSubtitles()
    {
        var args = FfmpegArguments.BuildRender(new RenderRequest(
            "v.mp4", "a.wav", "o.mp4", SubtitleMode.Hardsub, "sub.srt", LogoPath: "logo.png"));

        Filter(args).Should().Contain("overlay=").And.Contain("subtitles=");
        args.Should().ContainInOrder("-map", "[v]");
    }

    [Fact]
    public void Given_LogoAndSoftsub_When_BuildRender_Then_OverlaysAndMuxesSubtitle()
    {
        // Inputs: video(0), audio(1), srt(2), logo(3)
        var args = FfmpegArguments.BuildRender(new RenderRequest(
            "v.mp4", "a.wav", "o.mp4", SubtitleMode.Softsub, "sub.srt", LogoPath: "logo.png"));

        Filter(args).Should().Contain("[3:v]").And.Contain("overlay=");
        args.Should().ContainInOrder("-map", "2:s:0");
        args.Should().ContainInOrder("-c:s", "mov_text");
    }

    [Fact]
    public void Given_NoAudioAndLogo_When_BuildRender_Then_LogoIsSecondInputAndSourceAudioKept()
    {
        // Inputs: video(0), logo(1)
        var args = FfmpegArguments.BuildRender(new RenderRequest("v.mp4", null, "o.mp4", LogoPath: "logo.png"));

        Filter(args).Should().Contain("[1:v]");
        args.Should().ContainInOrder("-map", "0:a:0?");
    }
}
