using FluentValidation;

namespace Application.Features.Publishing.PublishJob;

public sealed class PublishJobCommandValidator : AbstractValidator<PublishJobCommand>
{
    public PublishJobCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.Targets).NotEmpty();
        RuleForEach(command => command.Targets).ChildRules(target =>
        {
            target.RuleFor(t => t.Platform).IsInEnum();
            target.RuleFor(t => t.Title).MaximumLength(200).When(t => t.Title is not null);
        });
    }
}
