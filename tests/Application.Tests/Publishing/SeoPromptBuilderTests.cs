using Domain.Entities;

namespace Application.Tests.Publishing;

public class SeoPromptBuilderTests
{
    [Fact]
    public void Given_Language_When_BuildSystemPrompt_Then_MentionsLanguageAndJsonSchema()
    {
        var prompt = SeoPromptBuilder.BuildSystemPrompt("vi");

        prompt.Should().Contain("vi").And.Contain("JSON").And.Contain("tags");
    }

    [Fact]
    public void Given_Segments_When_BuildUserPrompt_Then_IncludesResolvedTranscript()
    {
        var segment = new Segment(Guid.NewGuid(), 0, 0, 1, "hello");
        segment.SetAiTranslation("xin chào", null);

        var prompt = SeoPromptBuilder.BuildUserPrompt(new[] { segment });

        prompt.Should().Contain("xin chào");
    }
}
