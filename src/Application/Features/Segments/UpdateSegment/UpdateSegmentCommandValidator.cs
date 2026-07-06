using FluentValidation;

namespace Application.Features.Segments.UpdateSegment;

public sealed class UpdateSegmentCommandValidator : AbstractValidator<UpdateSegmentCommand>
{
    public UpdateSegmentCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.SegmentId).NotEmpty();
    }
}
