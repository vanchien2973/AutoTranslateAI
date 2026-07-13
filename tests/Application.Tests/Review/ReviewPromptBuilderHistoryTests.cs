using Domain.Entities;

namespace Application.Tests.Review;

public class ReviewPromptBuilderHistoryTests
{
    [Fact]
    public void BuildUserPrompt_WithHistory_IncludesConversationAndRequest()
    {
        var segment = new Segment(Guid.NewGuid(), 0, 0, 1, "hello");
        segment.SetAiTranslation("xin chào", "phụ đề");
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "earlier question"),
            new(ChatRole.Assistant, "earlier answer"),
        };

        var prompt = ReviewPromptBuilder.BuildUserPrompt([segment], history, "please refine segment 0");

        prompt.Should().Contain("Conversation so far:");
        prompt.Should().Contain("earlier question");
        prompt.Should().Contain("earlier answer");
        prompt.Should().Contain("please refine segment 0");
        prompt.Should().Contain("hello");
    }

    [Fact]
    public void BuildSystemPrompt_WithSubtitleDifferentFromAudio_MentionsBoth()
    {
        var prompt = ReviewPromptBuilder.BuildSystemPrompt("en", "vi", "fr");

        prompt.Should().Contain("vi (audio)").And.Contain("fr (subtitle)");
    }
}
