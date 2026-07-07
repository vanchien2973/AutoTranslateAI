using Application.Interfaces;
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
    }
}
