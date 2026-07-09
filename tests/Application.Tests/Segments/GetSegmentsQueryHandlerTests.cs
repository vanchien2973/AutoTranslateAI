using Application.Features.Segments.GetSegments;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Segments;

public class GetSegmentsQueryHandlerTests
{
    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);

        // Act
        var response = await new GetSegmentsQueryHandler(jobs).Handle(new GetSegmentsQuery(Guid.NewGuid(), 1, 50), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_Job_When_Handle_Then_ReturnsSegmentsOrderedByIndex()
    {
        // Arrange
        var job = TestJobs.AwaitingReview(TestJobs.Segment(2), TestJobs.Segment(0), TestJobs.Segment(1));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        var response = await new GetSegmentsQueryHandler(jobs).Handle(new GetSegmentsQuery(job.Id, 1, 50), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        response.Segments!.Items.Select(segment => segment.SegmentIndex).Should().Equal(0, 1, 2);
        response.Segments.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Given_SecondPage_When_Handle_Then_ReturnsThatSlice()
    {
        // Arrange
        var job = TestJobs.AwaitingReview(
            TestJobs.Segment(0), TestJobs.Segment(1), TestJobs.Segment(2), TestJobs.Segment(3));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        // Act — page 2, size 2 -> indexes 2,3
        var response = await new GetSegmentsQueryHandler(jobs).Handle(new GetSegmentsQuery(job.Id, 2, 2), CancellationToken.None);

        // Assert
        response.Segments!.Items.Select(segment => segment.SegmentIndex).Should().Equal(2, 3);
        response.Segments.Page.Should().Be(2);
        response.Segments.PageSize.Should().Be(2);
        response.Segments.TotalCount.Should().Be(4);
        response.Segments.TotalPages.Should().Be(2);
    }
}
