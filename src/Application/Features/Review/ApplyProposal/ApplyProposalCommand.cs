using MediatR;

namespace Application.Features.Review.ApplyProposal;

public sealed record ApplyProposalCommand(Guid JobId, Guid ProposalId) : IRequest<ApplyProposalResponse>;
