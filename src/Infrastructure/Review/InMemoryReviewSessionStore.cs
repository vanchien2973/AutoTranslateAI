using System.Collections.Concurrent;
using Application.Interfaces;

namespace Infrastructure.Review;

public sealed class InMemoryReviewSessionStore : IReviewSessionStore
{
    private sealed class Session
    {
        public List<ChatMessage> History { get; } = new();
        public ConcurrentDictionary<Guid, EditProposal> Proposals { get; } = new();
    }

    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public IReadOnlyList<ChatMessage> GetHistory(Guid jobId)
    {
        if (!_sessions.TryGetValue(jobId, out var session))
        {
            return [];
        }

        lock (session.History)
        {
            return session.History.ToList();
        }
    }

    public void AppendHistory(Guid jobId, ChatMessage message)
    {
        var session = _sessions.GetOrAdd(jobId, _ => new Session());
        lock (session.History)
        {
            session.History.Add(message);
        }
    }

    public void SaveProposals(Guid jobId, IEnumerable<EditProposal> proposals)
    {
        var session = _sessions.GetOrAdd(jobId, _ => new Session());
        foreach (var proposal in proposals)
        {
            session.Proposals[proposal.ProposalId] = proposal;
        }
    }

    public EditProposal? GetProposal(Guid jobId, Guid proposalId) =>
        _sessions.TryGetValue(jobId, out var session) && session.Proposals.TryGetValue(proposalId, out var proposal)
            ? proposal
            : null;

    public void RemoveProposal(Guid jobId, Guid proposalId)
    {
        if (_sessions.TryGetValue(jobId, out var session))
        {
            session.Proposals.TryRemove(proposalId, out _);
        }
    }
}
