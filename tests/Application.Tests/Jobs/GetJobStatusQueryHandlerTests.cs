using Application.Features.Jobs.GetJobStatus;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Jobs;

public class GetJobStatusQueryHandlerTests
{
    private static GetJobStatusQueryHandler Handler(IDubbingJobRepository jobs) =>
        new(jobs, Substitute.For<IStorageService>());

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);

        // Act
        var response = await Handler(jobs).Handle(new GetJobStatusQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
        response.Job.Should().BeNull();
    }

    [Fact]
    public async Task Given_AwaitingReviewJobWithEdits_When_Handle_Then_MapsCountsAndStatus()
    {
        // Arrange
        var edited = TestJobs.Segment(0);
        edited.EditAudioText("changed");
        var job = TestJobs.AwaitingReview(edited, TestJobs.Segment(1));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        var response = await Handler(jobs).Handle(new GetJobStatusQuery(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        response.Job!.Status.Should().Be("AwaitingReview");
        response.Job.SegmentCount.Should().Be(2);
        response.Job.EditedSegmentCount.Should().Be(1);
        response.Job.DownloadUrl.Should().BeNull();
    }
}
