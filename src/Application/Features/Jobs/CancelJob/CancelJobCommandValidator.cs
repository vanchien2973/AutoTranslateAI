using FluentValidation;

namespace Application.Features.Jobs.CancelJob;

public sealed class CancelJobCommandValidator : AbstractValidator<CancelJobCommand>
{
    public CancelJobCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
    }
}
