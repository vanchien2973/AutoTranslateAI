namespace Application.Features.Jobs.GetJobStatus;

public sealed record GetJobStatusResponse(OperationStatus Status, JobStatusDto? Job)
{
    public static GetJobStatusResponse Ok(JobStatusDto job) => new(OperationStatus.Ok, job);

    public static GetJobStatusResponse NotFound() => new(OperationStatus.NotFound, null);
}
