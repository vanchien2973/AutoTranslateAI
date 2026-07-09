using Application.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Segments.UpdateSegment;

public sealed class UpdateSegmentCommandHandler : IRequestHandler<UpdateSegmentCommand, UpdateSegmentResponse>
{
    private const string NotAwaitingReview = "Segments can only be edited while the job is awaiting review.";

    private readonly IDubbingJobRepository _jobs;

    public UpdateSegmentCommandHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<UpdateSegmentResponse> Handle(UpdateSegmentCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return UpdateSegmentResponse.NotFound();
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return UpdateSegmentResponse.Conflict(NotAwaitingReview);
        }

        var segment = job.Segments.FirstOrDefault(candidate => candidate.Id == request.SegmentId);
        if (segment is null)
        {
            return UpdateSegmentResponse.NotFound();
        }

        SegmentEditApplier.Apply(
            segment, request.AudioTextEdited, request.SubtitleTextEdited, request.SpeakerLabel, request.AssignedVoice);
        await _jobs.SaveChangesAsync(cancellationToken);

        return UpdateSegmentResponse.Ok(SegmentMapping.ToDto(segment));
    }
}
