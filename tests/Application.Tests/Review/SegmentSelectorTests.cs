using Domain.Entities;

namespace Application.Tests.Review;

public class SegmentSelectorTests
{
    private static IReadOnlyList<Segment> Segments(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new Segment(Guid.NewGuid(), index, index, index + 1, $"text {index}"))
            .ToList();

    [Fact]
    public void Given_MessageMentionsSegmentFive_When_Pick_Then_ReturnsFiveAndNeighbours()
    {
        // Arrange
        var segments = Segments(11);

        // Act
        var picked = SegmentSelector.Pick(segments, "dịch câu 5 tự nhiên hơn");

        // Assert
        picked.Select(segment => segment.SegmentIndex).Should().Equal(4, 5, 6);
    }

    [Fact]
    public void Given_MessageMentionsTwoSegments_When_Pick_Then_ReturnsBothWithNeighbours()
    {
        // Arrange
        var segments = Segments(15);

        // Act
        var picked = SegmentSelector.Pick(segments, "gộp câu 12 và 13 lại");

        // Assert
        picked.Select(segment => segment.SegmentIndex).Should().Equal(11, 12, 13, 14);
    }

    [Fact]
    public void Given_MessageHasNoNumbers_When_Pick_Then_ReturnsAllSegments()
    {
        // Arrange
        var segments = Segments(4);

        // Act
        var picked = SegmentSelector.Pick(segments, "dịch trang trọng hơn");

        // Assert
        picked.Should().HaveCount(4);
    }

    [Fact]
    public void Given_ReferencedNumbersMatchNothing_When_Pick_Then_FallsBackToAll()
    {
        // Arrange
        var segments = Segments(3);

        // Act
        var picked = SegmentSelector.Pick(segments, "câu 99");

        // Assert
        picked.Should().HaveCount(3);
    }
}
