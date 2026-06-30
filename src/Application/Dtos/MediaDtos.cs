namespace Application.Dtos;

public sealed record DownloadRequest(string Url, string OutputDirectory);
public sealed record DownloadResult(string FilePath, string? Title, double? DurationSeconds);
public sealed record DemucsRequest(
    string InputAudioPath,
    string OutputDirectory,
    string Model = "htdemucs",
    bool TwoStems = true);
public sealed record DemucsResult(string VocalsPath, string AccompanimentPath);
