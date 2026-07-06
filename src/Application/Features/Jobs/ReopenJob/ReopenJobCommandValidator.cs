using FluentValidation;

namespace Application.Features.Jobs.ReopenJob;

public sealed class ReopenJobCommandValidator : AbstractValidator<ReopenJobCommand>
{
    public ReopenJobCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
    }
}
