namespace Application.Features.Review.ApplyProposal;

public sealed record ApplyProposalResponse(ApplyProposalStatus Status, SegmentDto? Segment, string? Error)
{
    public static ApplyProposalResponse Ok(SegmentDto segment) => new(ApplyProposalStatus.Ok, segment, null);

    public static ApplyProposalResponse JobNotFound(Guid jobId) =>
        new(ApplyProposalStatus.JobNotFound, null, $"Job {jobId} was not found.");

    public static ApplyProposalResponse NotAwaitingReview() =>
        new(ApplyProposalStatus.NotAwaitingReview, null, "The job can only be edited while it is in the review state.");

    public static ApplyProposalResponse ProposalNotFound() =>
        new(ApplyProposalStatus.ProposalNotFound, null, "The proposal has expired or was already applied.");

    public static ApplyProposalResponse SegmentNotFound() =>
        new(ApplyProposalStatus.SegmentNotFound, null, "The segment no longer exists.");
}
