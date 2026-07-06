using Domain.Entities;

namespace Application.Tests;

// Shared builders for DubbingJob in specific states, driving the real state machine.
internal static class TestJobs
{
    public static Segment Segment(int index)
    {
        var segment = new Segment(Guid.NewGuid(), index, index, index + 1, $"original {index}");
        segment.SetAiTranslation($"ai {index}", $"sub {index}");
        return segment;
    }

    public static DubbingJob Queued() =>
        new("https://youtu.be/x", null, "en", "vi", "vi", true);

    public static DubbingJob AwaitingReview(params Segment[] segments)
    {
        var job = Queued();
        job.BeginPhase1Processing();
        job.SetSegments(segments);
        job.MarkAwaitingReview();
        return job;
    }

    public static DubbingJob Cancelled()
    {
        var job = Queued();
        job.Cancel();
        return job;
    }

    public static DubbingJob Completed(string outputFilePath = "output.mp4")
    {
        var job = Queued();
        job.BeginPhase1Processing();
        job.MarkAwaitingReview();
        job.Confirm();
        job.BeginPhase2Processing();
        job.Complete(outputFilePath);
        return job;
    }
}
