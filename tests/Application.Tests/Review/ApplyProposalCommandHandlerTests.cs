using Application.Features.Review.ApplyProposal;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Review;

public class ApplyProposalCommandHandlerTests
{
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
    public async Task Given_MissingProposal_When_Handle_Then_ReturnsProposalNotFoundAndSkipsJobLoad()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        var sessions = Substitute.For<IReviewSessionStore>();
        sessions.GetProposal(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((EditProposal?)null);
        var handler = new ApplyProposalCommandHandler(jobs, sessions);

        // Act
        var response = await handler.Handle(new ApplyProposalCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ApplyProposalStatus.ProposalNotFound);
        await jobs.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ValidProposal_When_Handle_Then_AppliesEditSavesAndRemovesProposal()
    {
        // Arrange
        var segment = Seg(0);
        var job = AwaitingReviewJob(segment);
        var proposal = new EditProposal(Guid.NewGuid(), segment.Id, 0, EditTarget.AudioText, segment.TtsText, "bản mới", "r");
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var sessions = Substitute.For<IReviewSessionStore>();
        sessions.GetProposal(job.Id, proposal.ProposalId).Returns(proposal);
        var handler = new ApplyProposalCommandHandler(jobs, sessions);

        // Act
        var response = await handler.Handle(new ApplyProposalCommand(job.Id, proposal.ProposalId), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ApplyProposalStatus.Ok);
        segment.AudioTextEdited.Should().Be("bản mới");
        segment.IsEdited.Should().BeTrue();
        response.Segment!.AudioTextEdited.Should().Be("bản mới");
        await jobs.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        sessions.Received(1).RemoveProposal(job.Id, proposal.ProposalId);
    }

    [Fact]
    public async Task Given_JobNotAwaitingReview_When_Handle_Then_ReturnsConflictAndDoesNotSave()
    {
        // Arrange
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true); // Queued
        var proposal = new EditProposal(Guid.NewGuid(), Guid.NewGuid(), 0, EditTarget.AudioText, "cur", "new", "r");
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var sessions = Substitute.For<IReviewSessionStore>();
        sessions.GetProposal(job.Id, proposal.ProposalId).Returns(proposal);
        var handler = new ApplyProposalCommandHandler(jobs, sessions);

        // Act
        var response = await handler.Handle(new ApplyProposalCommand(job.Id, proposal.ProposalId), CancellationToken.None);

        // Assert
        response.Status.Should().Be(ApplyProposalStatus.NotAwaitingReview);
        await jobs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
