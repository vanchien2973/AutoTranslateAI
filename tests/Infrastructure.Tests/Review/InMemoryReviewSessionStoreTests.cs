using Application.Dtos;
using Application.Enums;
using Infrastructure.Review;

namespace Infrastructure.Tests.Review;

public class InMemoryReviewSessionStoreTests
{
    private static EditProposal Proposal(Guid segmentId) =>
        new(Guid.NewGuid(), segmentId, 0, EditTarget.AudioText, "current", "proposed", "reason");

    [Fact]
    public void Given_AppendedMessages_When_GetHistory_Then_ReturnsInOrder()
    {
        // Arrange
        var store = new InMemoryReviewSessionStore();
        var jobId = Guid.NewGuid();

        // Act
        store.AppendHistory(jobId, new ChatMessage(ChatRole.User, "hi"));
        store.AppendHistory(jobId, new ChatMessage(ChatRole.Assistant, "hello"));

        // Assert
        var history = store.GetHistory(jobId);
        history.Should().HaveCount(2);
        history[0].Role.Should().Be(ChatRole.User);
        history[1].Content.Should().Be("hello");
    }

    [Fact]
    public void Given_SavedProposal_When_GetProposal_Then_ReturnsIt()
    {
        // Arrange
        var store = new InMemoryReviewSessionStore();
        var jobId = Guid.NewGuid();
        var proposal = Proposal(Guid.NewGuid());

        // Act
        store.SaveProposals(jobId, new[] { proposal });

        // Assert
        store.GetProposal(jobId, proposal.ProposalId).Should().Be(proposal);
    }

    [Fact]
    public void Given_RemovedProposal_When_GetProposal_Then_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryReviewSessionStore();
        var jobId = Guid.NewGuid();
        var proposal = Proposal(Guid.NewGuid());
        store.SaveProposals(jobId, new[] { proposal });

        // Act
        store.RemoveProposal(jobId, proposal.ProposalId);

        // Assert
        store.GetProposal(jobId, proposal.ProposalId).Should().BeNull();
    }

    [Fact]
    public void Given_ProposalOnAnotherJob_When_GetProposal_Then_IsolatedByJob()
    {
        // Arrange
        var store = new InMemoryReviewSessionStore();
        var proposal = Proposal(Guid.NewGuid());
        store.SaveProposals(Guid.NewGuid(), new[] { proposal });

        // Act / Assert
        store.GetProposal(Guid.NewGuid(), proposal.ProposalId).Should().BeNull();
    }

    [Fact]
    public void Given_NoSession_When_GetHistory_Then_ReturnsEmpty()
    {
        // Arrange
        var store = new InMemoryReviewSessionStore();

        // Act / Assert
        store.GetHistory(Guid.NewGuid()).Should().BeEmpty();
    }
}
