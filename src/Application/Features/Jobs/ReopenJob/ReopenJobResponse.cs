namespace Application.Features.Jobs.ReopenJob;

public sealed record ReopenJobResponse(OperationStatus Status, Guid JobId, string? JobStatus, string? Error)
{
    public static ReopenJobResponse Ok(Guid jobId, string jobStatus) => new(OperationStatus.Ok, jobId, jobStatus, null);

    public static ReopenJobResponse NotFound() => new(OperationStatus.NotFound, Guid.Empty, null, null);

    public static ReopenJobResponse Conflict(string error) => new(OperationStatus.Conflict, Guid.Empty, null, error);
}
