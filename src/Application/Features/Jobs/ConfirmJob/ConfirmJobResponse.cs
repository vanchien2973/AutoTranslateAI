namespace Application.Features.Jobs.ConfirmJob;

public sealed record ConfirmJobResponse(OperationStatus Status, Guid JobId, string? JobStatus, string? Error)
{
    public static ConfirmJobResponse Ok(Guid jobId, string jobStatus) => new(OperationStatus.Ok, jobId, jobStatus, null);

    public static ConfirmJobResponse NotFound() => new(OperationStatus.NotFound, Guid.Empty, null, null);

    public static ConfirmJobResponse Conflict(string error) => new(OperationStatus.Conflict, Guid.Empty, null, error);
}
