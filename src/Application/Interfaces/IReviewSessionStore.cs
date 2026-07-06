namespace Application.Interfaces;

public interface IReviewSessionStore
{
    IReadOnlyList<ChatMessage> GetHistory(Guid jobId);

    void AppendHistory(Guid jobId, ChatMessage message);

    void SaveProposals(Guid jobId, IEnumerable<EditProposal> proposals);

    EditProposal? GetProposal(Guid jobId, Guid proposalId);

    void RemoveProposal(Guid jobId, Guid proposalId);
}
