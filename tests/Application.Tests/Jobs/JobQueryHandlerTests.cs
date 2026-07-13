using Application.Features.Jobs.GetJobStatus;
using Application.Features.Review.GetReviewHistory;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Jobs;

public class GetJobStatusDownloadTests
{
    private static IStorageService StorageWith(string url)
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetPresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(url);
        return storage;
    }

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_NotFound()
    {
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);

        var response = await new GetJobStatusQueryHandler(jobs, StorageWith("x"))
            .Handle(new GetJobStatusQuery(Guid.NewGuid()), CancellationToken.None);

        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_CompletedJob_When_Handle_Then_EmbedsDownloadUrlAndFullProgress()
    {
        var job = TestJobs.Completed();
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var response = await new GetJobStatusQueryHandler(jobs, StorageWith("https://dl/out.mp4"))
            .Handle(new GetJobStatusQuery(job.Id), CancellationToken.None);

        response.Status.Should().Be(OperationStatus.Ok);
        response.Job!.ProgressPercent.Should().Be(100);
        response.Job.DownloadUrl.Should().Be("https://dl/out.mp4");
    }

    [Fact]
    public async Task Given_JobInReview_When_Handle_Then_NoDownloadUrl()
    {
        var job = TestJobs.AwaitingReview(TestJobs.Segment(0));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var response = await new GetJobStatusQueryHandler(jobs, StorageWith("unused"))
            .Handle(new GetJobStatusQuery(job.Id), CancellationToken.None);

        response.Job!.DownloadUrl.Should().BeNull();
        response.Job.ProgressPercent.Should().BeLessThan(100);
    }
}

public class GetReviewHistoryQueryHandlerTests
{
    [Fact]
    public async Task Given_History_When_Handle_Then_ReturnsPagedMessages()
    {
        var store = Substitute.For<IReviewSessionStore>();
        store.GetHistory(Arg.Any<Guid>()).Returns(new List<ChatMessage>
        {
            new(ChatRole.User, "please fix segment 0"),
            new(ChatRole.Assistant, "here is a suggestion"),
        });
        var handler = new GetReviewHistoryQueryHandler(store);

        var response = await handler.Handle(new GetReviewHistoryQuery(Guid.NewGuid(), 1, 50), CancellationToken.None);

        response.Messages.Items.Should().HaveCount(2);
        response.Messages.TotalCount.Should().Be(2);
    }
}
