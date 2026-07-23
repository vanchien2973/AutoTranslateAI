namespace Application.Features.Segments.GetVoicePreview;

public sealed record GetVoicePreviewResponse(OperationStatus Status, byte[]? Audio, string? ContentType)
{
    public static GetVoicePreviewResponse Ok(byte[] audio) => new(OperationStatus.Ok, audio, "audio/wav");

    public static GetVoicePreviewResponse NotFound() => new(OperationStatus.NotFound, null, null);

    public static GetVoicePreviewResponse Conflict() => new(OperationStatus.Conflict, null, null);
}
