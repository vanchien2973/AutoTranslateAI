using FluentValidation;

namespace Application.Features.Jobs.ConfirmJob;

public sealed class ConfirmJobCommandValidator : AbstractValidator<ConfirmJobCommand>
{
    public ConfirmJobCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
    }
}
