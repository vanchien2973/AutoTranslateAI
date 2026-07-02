using Application.Dtos;
using Infrastructure.Media.FFmpeg;

namespace Infrastructure.Tests.Media;

public class FfmpegComposeArgumentsTests
{
    [Fact]
    public void Given_TwoClips_When_BuildAssembleTimeline_Then_DelaysEachAndMixes()
    {
        // Arrange
        var request = new TimelineAssemblyRequest(
            [new TimelineClip("/tts/seg-0.wav", 0.0), new TimelineClip("/tts/seg-1.wav", 1.5)],
            "/work/vocals.wav");

        // Act
        var args = FfmpegArguments.BuildAssembleTimeline(request);
        var filter = args[args.ToList().IndexOf("-filter_complex") + 1];

        // Assert
        args.Should().Contain("/tts/seg-0.wav");
        args.Should().Contain("/tts/seg-1.wav");
        filter.Should().Contain("adelay=0:all=1");
        filter.Should().Contain("adelay=1500:all=1");
        filter.Should().Contain("amix=inputs=2:normalize=0");
        args.Should().ContainInOrder("-map", "[out]");
        args[^1].Should().Be("/work/vocals.wav");
    }

    [Fact]
    public void Given_NoClips_When_BuildAssembleTimeline_Then_Throws()
    {
        // Arrange
        var request = new TimelineAssemblyRequest([], "/work/vocals.wav");

        // Act
        var act = () => FfmpegArguments.BuildAssembleTimeline(request);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_MixRequest_When_BuildMix_Then_DucksBgmAndMixesTwoInputs()
    {
        // Arrange
        var request = new MixRequest("/work/vocals.wav", "/work/bgm.wav", "/work/mixed.wav");

        // Act
        var args = FfmpegArguments.BuildMix(request);
        var filter = args[args.ToList().IndexOf("-filter_complex") + 1];

        // Assert
        args.Should().ContainInOrder("-i", "/work/vocals.wav");
        args.Should().ContainInOrder("-i", "/work/bgm.wav");
        filter.Should().Contain("volume=-12dB");
        filter.Should().Contain("amix=inputs=2:duration=longest:normalize=0");
        args[^1].Should().Be("/work/mixed.wav");
    }

    [Fact]
    public void Given_RenderRequest_When_BuildRender_Then_MapsVideoAndNewAudio()
    {
        // Arrange
        var request = new RenderRequest("/work/source.mp4", "/work/mixed.wav", "/work/output.mp4");

        // Act
        var args = FfmpegArguments.BuildRender(request);

        // Assert
        args.Should().ContainInOrder("-map", "0:v:0");
        args.Should().ContainInOrder("-map", "1:a:0");
        args.Should().ContainInOrder("-c:v", "copy");
        args.Should().Contain("-shortest");
        args[^1].Should().Be("/work/output.mp4");
    }
}
