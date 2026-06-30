using Application.Dtos;
using Infrastructure.Media.Demucs;

namespace Infrastructure.Tests.Media;

public class DemucsArgumentsTests
{
    [Fact]
    public void Given_TwoStemsRequest_When_BuildSeparate_Then_AddsVocalsFlagAndInputLast()
    {
        // Arrange
        var request = new DemucsRequest("/work/audio.wav", "/work/stems", Model: "htdemucs", TwoStems: true);

        // Act
        var args = DemucsArguments.BuildSeparate(request);

        // Assert
        args.Should().ContainInOrder("-n", "htdemucs");
        args.Should().ContainInOrder("-o", "/work/stems");
        args.Should().ContainInOrder("--two-stems", "vocals");
        args[^1].Should().Be("/work/audio.wav");
    }

    [Fact]
    public void Given_FullSeparationRequest_When_BuildSeparate_Then_OmitsTwoStemsFlag()
    {
        // Arrange
        var request = new DemucsRequest("/work/audio.wav", "/work/stems", TwoStems: false);

        // Act
        var args = DemucsArguments.BuildSeparate(request);

        // Assert
        args.Should().NotContain("--two-stems");
    }
}
