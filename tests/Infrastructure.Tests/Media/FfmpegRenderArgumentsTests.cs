using Application.Dtos;
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

        // Assert
        args.Should().Contain("-vf");
        args.Should().Contain(arg => arg.StartsWith("subtitles=", StringComparison.Ordinal));
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

        // Assert — backslashes normalised, colon escaped, wrapped in quotes
        args.Should().Contain(@"subtitles='C\:/app/sub.srt'");
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
        args.Should().Contain("-vf");
        args.Should().NotContain("-c:v");             // video re-encoded, not copied
        args.Should().ContainInOrder("-c:a", "copy"); // source audio kept as-is
    }
}
