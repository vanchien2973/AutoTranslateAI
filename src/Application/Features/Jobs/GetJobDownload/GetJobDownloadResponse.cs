namespace Application.Features.Jobs.GetJobDownload;

public sealed record GetJobDownloadResponse(OperationStatus Status, string? Url, int ExpiresInSeconds, string? Error)
{
    public static GetJobDownloadResponse Ok(string url, int expiresInSeconds) =>
        new(OperationStatus.Ok, url, expiresInSeconds, null);

    public static GetJobDownloadResponse NotFound() => new(OperationStatus.NotFound, null, 0, null);

    public static GetJobDownloadResponse Conflict(string error) => new(OperationStatus.Conflict, null, 0, error);
}
