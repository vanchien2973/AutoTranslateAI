namespace Workers.Tests;

internal static class TestJobs
{
    private static DubbingJob New() => new("https://youtu.be/x", null, "en", "vi", "vi", true);
    
    public static DubbingJob AwaitingReview()
    {
        var job = New();
        job.BeginPhase1Processing();
        job.MarkAwaitingReview();
        return job;
    }

    public static DubbingJob Completed(string outputFilePath = "out.mp4")
    {
        var job = AwaitingReview();
        job.Confirm();
        job.BeginPhase2Processing();
        job.Complete(outputFilePath);
        return job;
    }
}
