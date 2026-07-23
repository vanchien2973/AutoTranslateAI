namespace Application.Features.Jobs.GetJobSource;

public sealed record GetJobSourceResponse(OperationStatus Status, string? FilePath, string? ContentType)
{
    public static GetJobSourceResponse Ok(string filePath, string contentType) =>
        new(OperationStatus.Ok, filePath, contentType);

    public static GetJobSourceResponse NotFound() => new(OperationStatus.NotFound, null, null);
}
