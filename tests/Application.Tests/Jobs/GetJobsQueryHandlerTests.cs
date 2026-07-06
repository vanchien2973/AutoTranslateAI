using Application.Features.Jobs.GetJobs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Jobs;

public class GetJobsQueryHandlerTests
{
    [Fact]
    public async Task Given_Jobs_When_Handle_Then_ReturnsPagedSummaries()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.ListAsync(0, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<DubbingJob>)new[] { TestJobs.Queued(), TestJobs.Queued() }, 5));

        // Act
        var response = await new GetJobsQueryHandler(jobs).Handle(new GetJobsQuery(1, 20), CancellationToken.None);

        // Assert
        response.Jobs.Items.Should().HaveCount(2);
        response.Jobs.TotalCount.Should().Be(5);
        response.Jobs.Page.Should().Be(1);
        response.Jobs.Items[0].Status.Should().Be("Queued");
    }

    [Fact]
    public async Task Given_OversizedPageSize_When_Handle_Then_ClampsToMax()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.ListAsync(0, 100, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<DubbingJob>)Array.Empty<DubbingJob>(), 0));

        // Act
        var response = await new GetJobsQueryHandler(jobs).Handle(new GetJobsQuery(1, 9999), CancellationToken.None);

        // Assert
        response.Jobs.PageSize.Should().Be(100);
        await jobs.Received(1).ListAsync(0, 100, Arg.Any<CancellationToken>());
    }
}
