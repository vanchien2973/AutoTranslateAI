using Application.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Review.ApplyProposal;

public sealed class ApplyProposalCommandHandler : IRequestHandler<ApplyProposalCommand, ApplyProposalResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly IReviewSessionStore _sessions;

    public ApplyProposalCommandHandler(IDubbingJobRepository jobs, IReviewSessionStore sessions)
    {
        _jobs = jobs;
        _sessions = sessions;
    }

    public async Task<ApplyProposalResponse> Handle(ApplyProposalCommand request, CancellationToken cancellationToken)
    {
        var proposal = _sessions.GetProposal(request.JobId, request.ProposalId);
        if (proposal is null)
        {
            return ApplyProposalResponse.ProposalNotFound();
        }

        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return ApplyProposalResponse.JobNotFound(request.JobId);
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return ApplyProposalResponse.NotAwaitingReview();
        }

        var segment = job.Segments.FirstOrDefault(candidate => candidate.Id == proposal.SegmentId);
        if (segment is null)
        {
            return ApplyProposalResponse.SegmentNotFound();
        }

        // Same write path as manual edits: sets IsEdited and flags NeedsTtsRegenerate so Phase 2 re-synths only this segment.
        if (proposal.Target == EditTarget.AudioText)
        {
            segment.EditAudioText(proposal.ProposedText);
        }
        else
        {
            segment.EditSubtitleText(proposal.ProposedText);
        }

        await _jobs.SaveChangesAsync(cancellationToken);
        _sessions.RemoveProposal(request.JobId, request.ProposalId);

        return ApplyProposalResponse.Ok(SegmentMapping.ToDto(segment));
    }
}
