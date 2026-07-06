using Application.Features.Jobs.GetJobDownload;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Jobs;

public class GetJobDownloadQueryHandlerTests
{
    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);
        var handler = new GetJobDownloadQueryHandler(jobs, Substitute.For<IStorageService>());

        // Act
        var response = await handler.Handle(new GetJobDownloadQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_OutputNotReady_When_Handle_Then_ReturnsConflict()
    {
        // Arrange
        var job = TestJobs.AwaitingReview();
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new GetJobDownloadQueryHandler(jobs, Substitute.For<IStorageService>());

        // Act
        var response = await handler.Handle(new GetJobDownloadQuery(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
    }

    [Fact]
    public async Task Given_CompletedJob_When_Handle_Then_ReturnsPresignedUrl()
    {
        // Arrange
        var job = TestJobs.Completed();
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var storage = Substitute.For<IStorageService>();
        storage.GetPresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns("https://signed");
        var handler = new GetJobDownloadQueryHandler(jobs, storage);

        // Act
        var response = await handler.Handle(new GetJobDownloadQuery(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        response.Url.Should().Be("https://signed");
        response.ExpiresInSeconds.Should().Be(3600);
    }
}
