using Application.Interfaces;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Segments.AdjustSegmentTiming;

public sealed class AdjustSegmentTimingCommandHandler : IRequestHandler<AdjustSegmentTimingCommand, AdjustSegmentTimingResponse>
{
    private const string NotAwaitingReview = "Segments can only be edited while the job is awaiting review.";

    private readonly IDubbingJobRepository _jobs;

    public AdjustSegmentTimingCommandHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<AdjustSegmentTimingResponse> Handle(AdjustSegmentTimingCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AdjustSegmentTimingResponse.NotFound();
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return AdjustSegmentTimingResponse.Conflict(NotAwaitingReview);
        }

        var segment = job.Segments.FirstOrDefault(candidate => candidate.Id == request.SegmentId);
        if (segment is null)
        {
            return AdjustSegmentTimingResponse.NotFound();
        }

        try
        {
            // Aggregate root enforces neighbour non-overlap; segment enforces start<end / non-negative.
            job.AdjustSegmentTiming(request.SegmentId, request.StartTime, request.EndTime);
        }
        catch (BusinessRuleViolationException exception)
        {
            return AdjustSegmentTimingResponse.Conflict(exception.Message);
        }

        await _jobs.SaveChangesAsync(cancellationToken);
        return AdjustSegmentTimingResponse.Ok(SegmentMapping.ToDto(segment));
    }
}
