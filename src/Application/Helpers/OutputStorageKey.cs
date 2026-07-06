namespace Application.Helpers;

public static class OutputStorageKey
{
    public static string For(Guid jobId) => $"{jobId:N}/output.mp4";
}
