namespace Application.Features.Jobs.CancelJob;

public sealed record CancelJobResponse(OperationStatus Status, Guid JobId, string? JobStatus, string? Error)
{
    public static CancelJobResponse Ok(Guid jobId, string jobStatus) => new(OperationStatus.Ok, jobId, jobStatus, null);

    public static CancelJobResponse NotFound() => new(OperationStatus.NotFound, Guid.Empty, null, null);

    public static CancelJobResponse Conflict(string error) => new(OperationStatus.Conflict, Guid.Empty, null, error);
}
