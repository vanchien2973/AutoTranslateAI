using Application.Interfaces;
using Domain.Entities;
using FluentValidation;

namespace Application.Features.Jobs.CreateJob;

public sealed class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator(ITtsService tts)
    {
        RuleFor(command => command.SourceUrl).NotEmpty();

        RuleFor(command => command.AudioLanguage!)
            .Must(tts.SupportsLanguage)
            .When(command => (command.EnableDubbing ?? true) && !string.IsNullOrWhiteSpace(command.AudioLanguage))
            .WithMessage(command => $"Audio language '{command.AudioLanguage}' has no supported text-to-speech voice.");

        RuleFor(command => command.LogoScalePercent!.Value)
            .InclusiveBetween(0.01, 1.0)
            .When(command => command.LogoScalePercent.HasValue)
            .WithMessage("Logo scale must be between 1% and 100% of the video height.");

        RuleFor(command => command.LogoMargin!.Value)
            .GreaterThanOrEqualTo(0)
            .When(command => command.LogoMargin.HasValue)
            .WithMessage("Logo margin must not be negative.");

        RuleForEach(command => command.AutoPublishTargets!)
            .ChildRules(target =>
            {
                target.RuleFor(t => t.Platform).IsInEnum();
                target.RuleFor(t => t.Title!)
                    .MaximumLength(JobPublishTarget.MaxTitleLength)
                    .When(t => t.Title is not null)
                    .WithMessage($"Publish title must be {JobPublishTarget.MaxTitleLength} characters or fewer.");
            })
            .When(command => command.AutoPublishTargets is not null);

        // One row per platform: the DB enforces it too, but a 400 reads better than a 500.
        RuleFor(command => command.AutoPublishTargets!)
            .Must(targets => targets.Select(target => target.Platform).Distinct().Count() == targets.Count)
            .When(command => command.AutoPublishTargets is { Count: > 0 })
            .WithMessage("Each platform can only be selected once.");
    }
}
