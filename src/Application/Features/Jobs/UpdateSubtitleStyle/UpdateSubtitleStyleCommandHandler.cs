using Application.Interfaces;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Jobs.UpdateSubtitleStyle;

public sealed class UpdateSubtitleStyleCommandHandler
    : IRequestHandler<UpdateSubtitleStyleCommand, UpdateSubtitleStyleResponse>
{
    private readonly IDubbingJobRepository _jobs;

    public UpdateSubtitleStyleCommandHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<UpdateSubtitleStyleResponse> Handle(
        UpdateSubtitleStyleCommand request,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return UpdateSubtitleStyleResponse.NotFound();
        }

        // Styling only feeds the Phase 2 render, so it is editable only while the job is paused for review.
        if (job.Status != JobStatus.AwaitingReview)
        {
            return UpdateSubtitleStyleResponse.Conflict(
                "Subtitle style can only be changed while the job is awaiting review.");
        }

        try
        {
            job.SetSubtitleStyle(request.FontFamily, request.FontSize, request.Position, request.Bold, request.Italic);
            await _jobs.SaveChangesAsync(cancellationToken);
        }
        catch (BusinessRuleViolationException exception)
        {
            return UpdateSubtitleStyleResponse.Conflict(exception.Message);
        }

        return UpdateSubtitleStyleResponse.Ok();
    }
}
