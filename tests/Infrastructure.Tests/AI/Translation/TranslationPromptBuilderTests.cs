using System.Text.Json;
using Infrastructure.AI.Translation;

namespace Infrastructure.Tests.AI.Translation;

public class TranslationPromptBuilderTests
{
    [Fact]
    public void Given_SourceAndTargetLang_When_BuildSystemPrompt_Then_MentionsLanguagesAndJsonShape()
    {
        // Act
        var prompt = TranslationPromptBuilder.BuildSystemPrompt("en", "vi");

        // Assert
        prompt.Should().Contain("en");
        prompt.Should().Contain("vi");
        prompt.Should().Contain("translations");
    }

    [Fact]
    public void Given_Texts_When_BuildUserPrompt_Then_ProducesJsonArrayUnderSegments()
    {
        // Arrange
        IReadOnlyList<string> texts = ["Hello", "World"];

        // Act
        var prompt = TranslationPromptBuilder.BuildUserPrompt(texts);

        // Assert
        using var document = JsonDocument.Parse(prompt);
        var segments = document.RootElement.GetProperty("segments");
        segments.GetArrayLength().Should().Be(2);
        segments[0].GetString().Should().Be("Hello");
    }
}
