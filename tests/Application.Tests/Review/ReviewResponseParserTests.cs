using Domain.Entities;

namespace Application.Tests.Review;

public class ReviewResponseParserTests
{
    private static IReadOnlyList<Segment> Segments()
    {
        var list = new List<Segment>();
        for (var index = 0; index < 6; index++)
        {
            var segment = new Segment(Guid.NewGuid(), index, index, index + 1, $"original {index}");
            segment.SetAiTranslation($"ai {index}", $"sub {index}");
            list.Add(segment);
        }

        return list;
    }

    [Fact]
    public void Given_ValidJson_When_TryParse_Then_MapsProposalToSegment()
    {
        // Arrange
        var segments = Segments();
        var json = """{"message":"ok","proposals":[{"segmentIndex":5,"target":"AudioText","proposedText":"bản mới","reason":"tự nhiên hơn"}]}""";

        // Act
        var success = ReviewResponseParser.TryParse(json, segments, out var message, out var proposals, out var error);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        message.Should().Be("ok");
        proposals.Should().ContainSingle();
        var proposal = proposals![0];
        proposal.SegmentId.Should().Be(segments[5].Id);
        proposal.SegmentIndex.Should().Be(5);
        proposal.Target.Should().Be(EditTarget.AudioText);
        proposal.CurrentText.Should().Be(segments[5].TtsText);
        proposal.ProposedText.Should().Be("bản mới");
        proposal.ProposalId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Given_InvalidJson_When_TryParse_Then_ReturnsFailure()
    {
        // Act
        var success = ReviewResponseParser.TryParse("not json {", Segments(), out _, out var proposals, out var error);

        // Assert
        success.Should().BeFalse();
        proposals.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Given_UnknownSegmentIndex_When_TryParse_Then_ReturnsFailure()
    {
        // Arrange
        var json = """{"message":"x","proposals":[{"segmentIndex":99,"target":"AudioText","proposedText":"y","reason":"z"}]}""";

        // Act
        var success = ReviewResponseParser.TryParse(json, Segments(), out _, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("99");
    }

    [Fact]
    public void Given_InvalidTarget_When_TryParse_Then_ReturnsFailure()
    {
        // Arrange
        var json = """{"message":"x","proposals":[{"segmentIndex":1,"target":"Foo","proposedText":"y","reason":"z"}]}""";

        // Act / Assert
        ReviewResponseParser.TryParse(json, Segments(), out _, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Given_MessageWithNoProposals_When_TryParse_Then_SucceedsWithEmptyProposals()
    {
        // Arrange
        var json = """{"message":"không cần sửa gì","proposals":[]}""";

        // Act
        var success = ReviewResponseParser.TryParse(json, Segments(), out var message, out var proposals, out _);

        // Assert
        success.Should().BeTrue();
        proposals.Should().BeEmpty();
        message.Should().Be("không cần sửa gì");
    }
}
