namespace Domain.Enums;

public enum JobStatus
{
    Queued = 0,
    DownloadingMedia = 1,
    ProcessingPhase1 = 2,
    AwaitingReview = 3,
    ConfirmedQueued = 4,
    ProcessingPhase2 = 5,
    Publishing = 6,
    Completed = 7,
    Failed = 8,
    Cancelled = 9,
}
