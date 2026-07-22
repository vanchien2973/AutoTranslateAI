using Application.Features.Languages.GetTranslationLanguages;
using Application.Interfaces;

namespace Application.Tests.Languages;

public class GetTranslationLanguagesQueryHandlerTests
{
    [Fact]
    public async Task Given_Provider_When_Handle_Then_ReturnsSubtitleLanguages()
    {
        // Arrange
        var translation = Substitute.For<ITranslationService>();
        translation.SupportedLanguages.Returns(new[] { "vi", "en", "de" });
        var handler = new GetTranslationLanguagesQueryHandler(translation);

        // Act
        var response = await handler.Handle(new GetTranslationLanguagesQuery(), CancellationToken.None);

        // Assert
        response.SubtitleLanguages.Should().BeEquivalentTo("vi", "en", "de");
    }
}
