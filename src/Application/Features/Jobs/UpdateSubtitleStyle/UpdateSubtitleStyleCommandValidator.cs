using Domain.Entities;
using FluentValidation;

namespace Application.Features.Jobs.UpdateSubtitleStyle;

public sealed class UpdateSubtitleStyleCommandValidator : AbstractValidator<UpdateSubtitleStyleCommand>
{
    public UpdateSubtitleStyleCommandValidator()
    {
        RuleFor(command => command.FontSize)
            .InclusiveBetween(DubbingJob.MinSubtitleFontSize, DubbingJob.MaxSubtitleFontSize)
            .WithMessage(
                $"Subtitle font size must be between {DubbingJob.MinSubtitleFontSize} and {DubbingJob.MaxSubtitleFontSize}.");

        RuleFor(command => command.Position).IsInEnum();
    }
}
