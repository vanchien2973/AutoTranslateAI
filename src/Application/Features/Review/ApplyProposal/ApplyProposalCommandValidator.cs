using FluentValidation;

namespace Application.Features.Review.ApplyProposal;

public sealed class ApplyProposalCommandValidator : AbstractValidator<ApplyProposalCommand>
{
    public ApplyProposalCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.ProposalId).NotEmpty();
    }
}
