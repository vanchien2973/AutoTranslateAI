namespace Application.Dtos;

public sealed record EditProposal(
    Guid ProposalId,
    Guid SegmentId,
    int SegmentIndex,
    EditTarget Target,
    string CurrentText,
    string ProposedText,
    string Reason);
