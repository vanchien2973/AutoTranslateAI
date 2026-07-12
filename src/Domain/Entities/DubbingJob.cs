using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Shared.Enums;

namespace Domain.Entities;

public sealed class DubbingJob : BaseEntity, IAuditableEntity, IVersioned
{
    // Valid transition table: from a status -> set of allowed next statuses.
    // Failed can go back to phases to RESUME from a failed step; Completed/Cancelled are terminal states.
    private static readonly IReadOnlyDictionary<JobStatus, JobStatus[]> AllowedTransitions =
        new Dictionary<JobStatus, JobStatus[]>
        {
            [JobStatus.Queued] = [JobStatus.DownloadingMedia, JobStatus.Cancelled],
            [JobStatus.DownloadingMedia] = [JobStatus.ProcessingPhase1, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.ProcessingPhase1] = [JobStatus.AwaitingReview, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.AwaitingReview] = [JobStatus.ConfirmedQueued, JobStatus.Cancelled],
            [JobStatus.ConfirmedQueued] = [JobStatus.ProcessingPhase2, JobStatus.Cancelled],
            [JobStatus.ProcessingPhase2] = [JobStatus.Publishing, JobStatus.Completed, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Publishing] = [JobStatus.Completed, JobStatus.Failed],
            [JobStatus.Failed] = [JobStatus.DownloadingMedia, JobStatus.ProcessingPhase1, JobStatus.ProcessingPhase2, JobStatus.Cancelled],
            [JobStatus.Completed] = [JobStatus.AwaitingReview, JobStatus.Publishing], // reopen to edit, or push the finished video to platforms
            [JobStatus.Cancelled] = [],
        };

    private readonly List<Segment> _segments = new();
    private readonly List<JobStep> _steps = new();

    private DubbingJob()
    {
    }

    public DubbingJob(
        string? sourceUrl,
        string? localFilePath,
        string? sourceLanguage,
        string audioLanguage,
        string? subtitleLanguage,
        bool enableDubbing,
        VoiceGender voiceGender = VoiceGender.Female,
        BgmMode bgmMode = BgmMode.DemucsAI,
        int duckingDb = -12,
        SubtitleMode subtitleMode = SubtitleMode.Softsub,
        Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl) && string.IsNullOrWhiteSpace(localFilePath))
        {
            throw new BusinessRuleViolationException("A job needs either a SourceUrl or a LocalFilePath.");
        }

        if (string.IsNullOrWhiteSpace(audioLanguage))
        {
            throw new BusinessRuleViolationException("AudioLanguage is required.");
        }

        SourceUrl = sourceUrl;
        LocalFilePath = localFilePath;
        SourceLanguage = sourceLanguage;
        AudioLanguage = audioLanguage;
        SubtitleLanguage = subtitleLanguage;
        EnableDubbing = enableDubbing;
        VoiceGender = voiceGender;
        BgmMode = bgmMode;
        DuckingDb = duckingDb;
        SubtitleMode = subtitleMode;
        UserId = userId;
        Status = JobStatus.Queued;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string? SourceUrl { get; private set; }
    public string? LocalFilePath { get; private set; }
    public string? SourceLanguage { get; private set; }
    public string AudioLanguage { get; private set; } = string.Empty;
    public string? SubtitleLanguage { get; private set; }
    public bool EnableDubbing { get; private set; }
    public VoiceGender VoiceGender { get; private set; }
    public BgmMode BgmMode { get; private set; }
    public int DuckingDb { get; private set; }
    public SubtitleMode SubtitleMode { get; private set; }
    public JobStatus Status { get; private set; }
    public StepType? CurrentStep { get; private set; }
    public int ProgressPercent { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? OutputFilePath { get; private set; }
    public string? WorkspacePath { get; private set; }
    public Guid? UserId { get; private set; }
    public int RowVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReviewReadyAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public IReadOnlyCollection<Segment> Segments => _segments.AsReadOnly();
    public IReadOnlyCollection<JobStep> Steps => _steps.AsReadOnly();

    // ---- State machine: each method is a valid transition ----

    /// <summary>The worker accepts the job and starts downloading media (Queued/Failed -> DownloadingMedia).</summary>
    public void StartDownload()
    {
        TransitionTo(JobStatus.DownloadingMedia);
        StartedAt ??= DateTimeOffset.UtcNow;
        CurrentStep = StepType.Download;
    }

    /// <summary>Enter Phase 1 to process audio/transcribe/translate (DownloadingMedia/Failed -> ProcessingPhase1).</summary>
    public void StartPhase1() => TransitionTo(JobStatus.ProcessingPhase1);

    /// <summary>
    /// Put the job into a state that is processing Phase 1, making it idempotent when calling it again — used for workers when (re)start/resume: 
    /// a message being retryed might see the job is in Processing Phase 1 (previously failed to be recorded) and therefore must be skipped, avoiding incorrect transitions.
    /// </summary>
    public void BeginPhase1Processing()
    {
        switch (Status)
        {
            case JobStatus.ProcessingPhase1:
                return; // Already processing (a re-delivered/retried message); nothing to transition.
            case JobStatus.Queued:
                TransitionTo(JobStatus.DownloadingMedia);
                TransitionTo(JobStatus.ProcessingPhase1);
                break;
            case JobStatus.DownloadingMedia:
            case JobStatus.Failed:
                TransitionTo(JobStatus.ProcessingPhase1);
                break;
            default:
                throw new InvalidStateTransitionException(nameof(DubbingJob), Status, JobStatus.ProcessingPhase1);
        }

        StartedAt ??= DateTimeOffset.UtcNow;
        CurrentStep = StepType.Download;
    }

    /// <summary>Phase 1 completed — stop point for user review; DO NOT publish anything (ProcessingPhase1 -> AwaitingReview).</summary>
    public void MarkAwaitingReview()
    {
        TransitionTo(JobStatus.AwaitingReview);
        ReviewReadyAt = DateTimeOffset.UtcNow;
        CurrentStep = null;
    }

    /// <summary>User confirms the transcript, allowing Phase 2 to run (AwaitingReview -> ConfirmedQueued).</summary>
    public void Confirm()
    {
        TransitionTo(JobStatus.ConfirmedQueued);
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reopen a completed job to edit the transcript and then run Phase 2 again (Completed -> AwaitingReview). 
    /// Reset the Phase 2 steps to Pending so they run again; Phase 1 remains unchanged (do not re-transcribe). 
    /// This ensures that when confirming, only the synth segment with NeedsTtsRegenerate (edited) is present in the TTS; the rest reuses the old clip
    /// </summary>
    public void ReopenForReview()
    {
        // Reopen is only meaningful for a finished job; the Phase-1 -> AwaitingReview path is MarkAwaitingReview.
        if (Status != JobStatus.Completed)
        {
            throw new InvalidStateTransitionException(nameof(DubbingJob), Status, JobStatus.AwaitingReview);
        }

        TransitionTo(JobStatus.AwaitingReview);

        foreach (var step in _steps.Where(step => step.Phase == 2))
        {
            step.Reset();
        }

        OutputFilePath = null;
        CompletedAt = null;
        CurrentStep = null;
        ReviewReadyAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>The worker receives the Phase 2 message and starts TTS/Mix/Render (ConfirmedQueued/Failed -> ProcessingPhase2).</summary>
    public void StartPhase2() => TransitionTo(JobStatus.ProcessingPhase2);

    /// <summary>
    /// Move the job to Processing Phase 2, ensuring safety when calling it back (idempotent) — used for Phase2Consumer when receiving messages/resumes: 
    /// if a message is retryed, the process can see that the job is already in ProcessingPhase 2 (it wasn't marked Failed the previous time) so it's skipped, preventing incorrect transitions.
    /// </summary>
    public void BeginPhase2Processing()
    {
        switch (Status)
        {
            case JobStatus.ProcessingPhase2:
                return; // Already processing Phase 2 (a re-delivered/retried message).
            case JobStatus.ConfirmedQueued:
            case JobStatus.Failed:
                TransitionTo(JobStatus.ProcessingPhase2);
                break;
            default:
                throw new InvalidStateTransitionException(nameof(DubbingJob), Status, JobStatus.ProcessingPhase2);
        }

        CurrentStep = StepType.Tts;
    }

    /// <summary>Start uploading/publishing results (ProcessingPhase2 -> Publishing).</summary>
    public void StartPublishing()
    {
        TransitionTo(JobStatus.Publishing);
        CurrentStep = StepType.Publish;
    }

    /// <summary>Complete the job and save the output file (ProcessingPhase2/Publishing -> Completed).</summary>
    public void Complete(string outputFilePath)
    {
        TransitionTo(JobStatus.Completed);
        OutputFilePath = outputFilePath;
        ProgressPercent = 100;
        CurrentStep = null;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Mark the job as failed and save the error message; it can be resumed later (-> Failed).</summary>
    public void Fail(string error)
    {
        TransitionTo(JobStatus.Failed);
        ErrorMessage = error;
        Touch();
    }

    /// <summary>User cancels the job. Valid in most running states, except when already finished (-> Cancelled).</summary>
    public void Cancel()
    {
        TransitionTo(JobStatus.Cancelled);
        Touch();
    }

    // ---- Non-transition mutations ----

    /// <summary>Update progress (current step + %). Does not change the state machine status.</summary>
    public void UpdateProgress(StepType currentStep, int progressPercent)
    {
        CurrentStep = currentStep;
        ProgressPercent = Math.Clamp(progressPercent, 0, 100);
        Touch();
    }

    /// <summary>Set the temporary workspace directory for the job (created by IWorkspaceManager).</summary>
    public void SetWorkspace(string workspacePath)
    {
        WorkspacePath = workspacePath;
        Touch();
    }

    /// <summary>Override all segments (the Transcribe step creates a new transcript).</summary>
    public void SetSegments(IEnumerable<Segment> segments)
    {
        _segments.Clear();
        _segments.AddRange(segments);
        Touch();
    }

    /// <summary>Get the JobStep for a stage, creating a new one if it doesn't exist — used to track/resume by StepType.</summary>
    public JobStep GetOrCreateStep(StepType stepType, int phase)
    {
        var existing = _steps.Find(s => s.StepType == stepType);
        if (existing is not null)
        {
            return existing;
        }

        var step = new JobStep(Id, stepType, phase);
        _steps.Add(step);
        return step;
    }

    /// <summary>
    /// Adjust timing for 1 segment; aggregate root maintains invariance across segments: no overlapping of the preceding/following segment.
    /// </summary>
    public void AdjustSegmentTiming(Guid segmentId, double startTime, double endTime)
    {
        var ordered = _segments.OrderBy(segment => segment.SegmentIndex).ToList();
        var position = ordered.FindIndex(segment => segment.Id == segmentId);
        if (position < 0)
        {
            throw new BusinessRuleViolationException($"Segment {segmentId} does not belong to this job.");
        }

        var previousEnd = position > 0 ? ordered[position - 1].EndTime : 0.0;
        if (startTime < previousEnd)
        {
            throw new BusinessRuleViolationException("Segment start overlaps the previous segment.");
        }

        if (position < ordered.Count - 1 && endTime > ordered[position + 1].StartTime)
        {
            throw new BusinessRuleViolationException("Segment end overlaps the next segment.");
        }

        // Segment enforces its own start<end / non-negative invariant.
        ordered[position].AdjustTiming(startTime, endTime);
        Touch();
    }

    // Central guard: validates the requested transition against AllowedTransitions before applying it.
    private void TransitionTo(JobStatus target)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || Array.IndexOf(allowed, target) < 0)
        {
            throw new InvalidStateTransitionException(nameof(DubbingJob), Status, target);
        }

        Status = target;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
