using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class JobStep : BaseEntity
{
    private JobStep()
    {
    }

    public JobStep(Guid jobId, StepType stepType, int phase)
    {
        JobId = jobId;
        StepType = stepType;
        Phase = phase;
        Status = JobStepStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid JobId { get; private set; }

    public StepType StepType { get; private set; }

    public JobStepStatus Status { get; private set; }
    public int Phase { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long? DurationMs { get; private set; }
    public string? OutputPath { get; private set; }

    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Start the step. If this is a retry after a failure, increment RetryCount and clear the previous error.</summary>
    public void Start()
    {
        if (Status is JobStepStatus.Completed)
        {
            throw new InvalidStateTransitionException(nameof(JobStep), Status, JobStepStatus.Running);
        }

        // A restart after a failure counts as a retry.
        if (Status is JobStepStatus.Failed)
        {
            RetryCount++;
        }

        Status = JobStepStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
        CompletedAt = null;
        DurationMs = null;
        ErrorMessage = null;
    }

    /// <summary>Mark the step as completed and save the output file; calculate the duration.</summary>
    public void Complete(string? outputPath = null)
    {
        if (Status is not JobStepStatus.Running)
        {
            throw new InvalidStateTransitionException(nameof(JobStep), Status, JobStepStatus.Completed);
        }

        Status = JobStepStatus.Completed;
        OutputPath = outputPath;
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = ElapsedMs();
    }

    /// <summary>Mark the step as failed and save the error message; this is the resume point for the pipeline.</summary>
    public void Fail(string error)
    {
        Status = JobStepStatus.Failed;
        ErrorMessage = error;
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = ElapsedMs();
    }

    /// <summary>Bỏ qua step (vd không lồng tiếng thì bỏ Tts). Hợp lệ khi step chưa xong — kể cả đang Running rồi mới quyết định skip.</summary>
    public void Skip()
    {
        // A step may only decide to skip after it has started (Running); only a Completed step can't be un-done.
        if (Status is JobStepStatus.Completed)
        {
            throw new InvalidStateTransitionException(nameof(JobStep), Status, JobStepStatus.Skipped);
        }

        Status = JobStepStatus.Skipped;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Put the step in Pending to restart (use when reopening review — reset the Phase 2 steps). Keep RetryCount as history.</summary>
    public void Reset()
    {
        Status = JobStepStatus.Pending;
        StartedAt = null;
        CompletedAt = null;
        DurationMs = null;
        ErrorMessage = null;
    }

    // Elapsed since StartedAt in ms; 0 if the step never started.
    private long ElapsedMs() =>
        StartedAt is { } started
            ? (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds
            : 0;
}
