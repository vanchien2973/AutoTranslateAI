using Application.Helpers;
using Domain.Entities;

namespace Application.Tests.Review;

public class ReviewSegmentNumberingTests
{
    private static List<Segment> Segments(int count = 3)
    {
        var list = new List<Segment>();
        for (var index = 0; index < count; index++)
        {
            var segment = new Segment(Guid.NewGuid(), index, index, index + 1, $"original {index}");
            segment.SetAiTranslation($"ai {index}", $"sub {index}");
            list.Add(segment);
        }

        return list;
    }

    [Fact]
    public void Given_Segments_When_BuildUserPrompt_Then_NumbersThemFromOne()
    {
        var prompt = ReviewPromptBuilder.BuildUserPrompt(Segments(), [], "sửa câu đầu");

        prompt.Should().Contain("[1] source: \"original 0\"");
        prompt.Should().Contain("[3] source: \"original 2\"");
        prompt.Should().NotContain("[0]");
    }

    [Fact]
    public void Given_ModelSaysSegmentOne_When_TryParse_Then_TargetsTheFirstSegment()
    {
        var segments = Segments();
        var json =
            """{"message":"ok","proposals":[{"segmentIndex":1,"target":"AudioText","proposedText":"mới","reason":"r"}]}""";

        var parsed = ReviewResponseParser.TryParse(json, segments, out _, out var proposals, out var error);

        parsed.Should().BeTrue(error);
        var proposal = proposals.Should().ContainSingle().Subject;
        proposal.SegmentId.Should().Be(segments[0].Id, "segment 1 on screen is the first segment");
        proposal.SegmentIndex.Should().Be(0, "the DTO keeps the 0-based domain index");
        proposal.CurrentText.Should().Be(segments[0].TtsText);
    }

    [Fact]
    public void Given_ModelSaysSegmentZero_When_TryParse_Then_Rejected()
    {
        var json =
            """{"message":"x","proposals":[{"segmentIndex":0,"target":"AudioText","proposedText":"y","reason":"z"}]}""";

        var parsed = ReviewResponseParser.TryParse(json, Segments(), out _, out _, out var error);

        parsed.Should().BeFalse();
        error.Should().Contain("0");
    }

    [Fact]
    public void Given_IndexPastTheEnd_When_TryParse_Then_ReportsTheNumberTheUserWouldSee()
    {
        var json =
            """{"message":"x","proposals":[{"segmentIndex":4,"target":"AudioText","proposedText":"y","reason":"z"}]}""";

        var parsed = ReviewResponseParser.TryParse(json, Segments(), out _, out _, out var error);

        parsed.Should().BeFalse();
        error.Should().Contain("4", "the message must quote the number shown on screen, not the offset");
    }
}
