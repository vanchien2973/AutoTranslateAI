using FluentValidation;

namespace Application.Features.Segments.BulkUpdateSegments;

public sealed class BulkUpdateSegmentsCommandValidator : AbstractValidator<BulkUpdateSegmentsCommand>
{
    public BulkUpdateSegmentsCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.Segments).NotEmpty();
    }
}
