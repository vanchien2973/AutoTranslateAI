using Application.Dtos;
using Infrastructure.Media.FFmpeg;

namespace Infrastructure.Tests.Media;

public class FfmpegArgumentsTests
{
    [Fact]
    public void Given_DefaultRequest_When_BuildExtractAudio_Then_Produces16kMonoPcmWav()
    {
        // Arrange
        var request = new AudioExtractionRequest("/work/source.mp4", "/work/audio.wav");

        // Act
        var args = FfmpegArguments.BuildExtractAudio(request);

        // Assert
        args.Should().ContainInOrder("-i", "/work/source.mp4");
        args.Should().Contain("-vn");
        args.Should().ContainInOrder("-acodec", "pcm_s16le");
        args.Should().ContainInOrder("-ar", "16000");
        args.Should().ContainInOrder("-ac", "1");
        args[0].Should().Be("-y");
        args[^1].Should().Be("/work/audio.wav");
    }

    [Fact]
    public void Given_CustomSampleRateAndChannels_When_BuildExtractAudio_Then_UsesThem()
    {
        // Arrange
        var request = new AudioExtractionRequest("/in.mkv", "/out.wav", SampleRate: 48000, Channels: 2);

        // Act
        var args = FfmpegArguments.BuildExtractAudio(request);

        // Assert
        args.Should().ContainInOrder("-ar", "48000");
        args.Should().ContainInOrder("-ac", "2");
    }
}
