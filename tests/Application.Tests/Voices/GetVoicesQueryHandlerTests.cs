using Application.Features.Voices.GetVoices;
using Application.Interfaces;
using Shared.Enums;

namespace Application.Tests.Voices;

public class GetVoicesQueryHandlerTests
{
    [Fact]
    public async Task Given_Language_When_Handle_Then_ReturnsVoicesFromTtsService()
    {
        // Arrange
        var voices = new VoiceInfo[]
        {
            new("vi-VN-HoaiMyNeural", "vi", VoiceGender.Female, "vi-VN-HoaiMyNeural"),
            new("vi-VN-NamMinhNeural", "vi", VoiceGender.Male, "vi-VN-NamMinhNeural"),
        };
        var tts = Substitute.For<ITtsService>();
        tts.ListVoicesAsync("vi", Arg.Any<CancellationToken>()).Returns(voices);
        var handler = new GetVoicesQueryHandler(tts);

        // Act
        var response = await handler.Handle(new GetVoicesQuery("vi"), CancellationToken.None);

        // Assert
        response.Voices.Should().HaveCount(2);
        response.Voices.Select(voice => voice.Gender).Should().Contain([VoiceGender.Female, VoiceGender.Male]);
    }
}
