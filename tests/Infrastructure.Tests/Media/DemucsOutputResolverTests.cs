using Application.Dtos;
using Infrastructure.Media.Demucs;

namespace Infrastructure.Tests.Media;

public class DemucsOutputResolverTests
{
    [Fact]
    public void Given_TwoStemsRequest_When_Resolve_Then_PointsAtVocalsAndNoVocalsUnderModelTrackFolder()
    {
        // Arrange
        var request = new DemucsRequest("/work/audio.wav", "/work/stems", Model: "htdemucs", TwoStems: true);

        // Act
        var result = DemucsOutputResolver.Resolve(request);

        // Assert
        result.VocalsPath.Should().Be(Path.Combine("/work/stems", "htdemucs", "audio", "vocals.wav"));
        result.AccompanimentPath.Should().Be(Path.Combine("/work/stems", "htdemucs", "audio", "no_vocals.wav"));
    }

    [Fact]
    public void Given_FullSeparationRequest_When_Resolve_Then_UsesOtherWavForAccompaniment()
    {
        // Arrange
        var request = new DemucsRequest("/work/audio.wav", "/work/stems", TwoStems: false);

        // Act
        var result = DemucsOutputResolver.Resolve(request);

        // Assert
        result.AccompanimentPath.Should().EndWith(Path.Combine("audio", "other.wav"));
    }
}
