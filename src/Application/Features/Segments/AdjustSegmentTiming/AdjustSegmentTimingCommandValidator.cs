using FluentValidation;

namespace Application.Features.Segments.AdjustSegmentTiming;

public sealed class AdjustSegmentTimingCommandValidator : AbstractValidator<AdjustSegmentTimingCommand>
{
    public AdjustSegmentTimingCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.SegmentId).NotEmpty();
        RuleFor(command => command.StartTime).GreaterThanOrEqualTo(0);
        RuleFor(command => command.EndTime).GreaterThan(command => command.StartTime);
    }
}
