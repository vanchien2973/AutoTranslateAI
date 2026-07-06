using Application.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Segments.BulkUpdateSegments;

public sealed class BulkUpdateSegmentsCommandHandler : IRequestHandler<BulkUpdateSegmentsCommand, BulkUpdateSegmentsResponse>
{
    private const string NotAwaitingReview = "Segments can only be edited while the job is awaiting review.";

    private readonly IDubbingJobRepository _jobs;

    public BulkUpdateSegmentsCommandHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<BulkUpdateSegmentsResponse> Handle(BulkUpdateSegmentsCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return BulkUpdateSegmentsResponse.NotFound($"Job {request.JobId} was not found.");
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return BulkUpdateSegmentsResponse.Conflict(NotAwaitingReview);
        }

        var byId = job.Segments.ToDictionary(segment => segment.Id);
        foreach (var edit in request.Segments)
        {
            if (!byId.TryGetValue(edit.SegmentId, out var segment))
            {
                return BulkUpdateSegmentsResponse.NotFound($"Segment {edit.SegmentId} not found in job {request.JobId}.");
            }

            SegmentEditApplier.Apply(segment, edit.AudioTextEdited, edit.SubtitleTextEdited, edit.AssignedVoice);
        }

        await _jobs.SaveChangesAsync(cancellationToken);

        var segments = job.Segments
            .OrderBy(segment => segment.SegmentIndex)
            .Select(SegmentMapping.ToDto)
            .ToList();

        return BulkUpdateSegmentsResponse.Ok(segments);
    }
}
