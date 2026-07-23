namespace Application.Features.Jobs.UpdateSubtitleStyle;

public sealed record UpdateSubtitleStyleResponse(OperationStatus Status, string? Error)
{
    public static UpdateSubtitleStyleResponse Ok() => new(OperationStatus.Ok, null);

    public static UpdateSubtitleStyleResponse NotFound() => new(OperationStatus.NotFound, null);

    public static UpdateSubtitleStyleResponse Conflict(string error) => new(OperationStatus.Conflict, error);
}
