using Application.Features.Review.ReviewChat;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Review;

public class ReviewChatCommandHandlerTests
{
    private const string ValidJson =
        """{"message":"đã sửa","proposals":[{"segmentIndex":1,"target":"AudioText","proposedText":"bản mới","reason":"tự nhiên hơn"}]}""";

    private static Segment Seg(int index)
    {
        var segment = new Segment(Guid.NewGuid(), index, index, index + 1, $"original {index}");
        segment.SetAiTranslation($"ai {index}", $"sub {index}");
        return segment;
    }

    private static DubbingJob AwaitingReviewJob(params Segment[] segments)
    {
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true);
        job.BeginPhase1Processing();
        job.SetSegments(segments);
        job.MarkAwaitingReview();
        return job;
    }

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsJobNotFoundAndSkipsLlm()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);
        var llm = Substitute.For<ILlmCompletionService>();
        var handler = new ReviewChatCommandHandler(jobs, llm, Substitute.For<IReviewSessionStore>());

        // Act
        var response = await handler.Handle(new ReviewChatCommand(Guid.NewGuid(), "hi"), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ReviewChatStatus.JobNotFound);
        await llm.DidNotReceive().CompleteJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_JobNotAwaitingReview_When_Handle_Then_ReturnsConflictAndSkipsLlm()
    {
        // Arrange
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true); // Queued
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var llm = Substitute.For<ILlmCompletionService>();
        var handler = new ReviewChatCommandHandler(jobs, llm, Substitute.For<IReviewSessionStore>());

        // Act
        var response = await handler.Handle(new ReviewChatCommand(job.Id, "hi"), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ReviewChatStatus.NotAwaitingReview);
        await llm.DidNotReceive().CompleteJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ValidLlmResponse_When_Handle_Then_ReturnsProposalsAndPersistsSession()
    {
        // Arrange
        var segment = Seg(0);
        var job = AwaitingReviewJob(segment);
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var llm = Substitute.For<ILlmCompletionService>();
        llm.CompleteJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValidJson);
        var sessions = Substitute.For<IReviewSessionStore>();
        sessions.GetHistory(Arg.Any<Guid>()).Returns(Array.Empty<ChatMessage>());
        var handler = new ReviewChatCommandHandler(jobs, llm, sessions);

        // Act
        var response = await handler.Handle(new ReviewChatCommand(job.Id, "sửa câu 0"), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ReviewChatStatus.Ok);
        response.AssistantMessage.Should().Be("đã sửa");
        response.Proposals.Should().ContainSingle();
        response.Proposals[0].SegmentId.Should().Be(segment.Id);
        response.Proposals[0].ProposedText.Should().Be("bản mới");
        sessions.Received(1).SaveProposals(job.Id, Arg.Any<IEnumerable<EditProposal>>());
        sessions.Received(1).AppendHistory(job.Id, Arg.Is<ChatMessage>(message => message.Role == ChatRole.User));
        sessions.Received(1).AppendHistory(job.Id, Arg.Is<ChatMessage>(message => message.Role == ChatRole.Assistant));
    }

    [Fact]
    public async Task Given_LlmReturnsInvalidJson_When_Handle_Then_ReturnsInvalidResponseAndPersistsNothing()
    {
        // Arrange
        var job = AwaitingReviewJob(Seg(0));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var llm = Substitute.For<ILlmCompletionService>();
        llm.CompleteJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("not json");
        var sessions = Substitute.For<IReviewSessionStore>();
        sessions.GetHistory(Arg.Any<Guid>()).Returns(Array.Empty<ChatMessage>());
        var handler = new ReviewChatCommandHandler(jobs, llm, sessions);

        // Act
        var response = await handler.Handle(new ReviewChatCommand(job.Id, "sửa"), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ReviewChatStatus.InvalidResponse);
        sessions.DidNotReceive().SaveProposals(Arg.Any<Guid>(), Arg.Any<IEnumerable<EditProposal>>());
    }
}
