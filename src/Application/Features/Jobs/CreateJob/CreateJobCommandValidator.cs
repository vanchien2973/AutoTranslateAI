using FluentValidation;

namespace Application.Features.Jobs.CreateJob;

public sealed class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(command => command.SourceUrl).NotEmpty();
    }
}
