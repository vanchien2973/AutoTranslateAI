using Domain.Entities;

namespace Application.Tests.Review;

public class ReviewPromptBuilderTests
{
    [Fact]
    public void Given_SourceAndTargetLanguages_When_BuildSystemPrompt_Then_MentionsBothAndJson()
    {
        // Act
        var prompt = ReviewPromptBuilder.BuildSystemPrompt("en", "vi", "vi");

        // Assert
        prompt.Should().Contain("en").And.Contain("vi").And.Contain("JSON");
    }

    [Fact]
    public void Given_SegmentsAndMessage_When_BuildUserPrompt_Then_IncludesIndexOriginalAndRequest()
    {
        // Arrange
        var segment = new Segment(Guid.NewGuid(), 5, 5, 6, "hello world");

        // Act
        var prompt = ReviewPromptBuilder.BuildUserPrompt(new[] { segment }, Array.Empty<ChatMessage>(), "dịch câu 5");

        // Assert
        // Segment 5 is shown as "[6]": the prompt numbers from 1 so it matches the review table.
        prompt.Should().Contain("[6]").And.Contain("hello world").And.Contain("dịch câu 5");
    }
}
