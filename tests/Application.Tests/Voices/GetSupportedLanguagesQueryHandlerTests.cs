using Application.Features.Voices.GetSupportedLanguages;
using Application.Interfaces;

namespace Application.Tests.Voices;

public class GetSupportedLanguagesQueryHandlerTests
{
    [Fact]
    public async Task Given_Provider_When_Handle_Then_ReturnsSupportedLanguages()
    {
        // Arrange
        var tts = Substitute.For<ITtsService>();
        tts.SupportedLanguages.Returns(new[] { "vi", "en", "ja" });
        var handler = new GetSupportedLanguagesQueryHandler(tts);

        // Act
        var response = await handler.Handle(new GetSupportedLanguagesQuery(), CancellationToken.None);

        // Assert
        response.AudioLanguages.Should().BeEquivalentTo("vi", "en", "ja");
    }
}
