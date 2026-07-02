namespace Infrastructure.Media.Downloader;

internal static class YtDlpOutputParser
{
    public static string? ParseFinalPath(string standardOutput)
    {
        if (string.IsNullOrWhiteSpace(standardOutput))
        {
            return null;
        }

        return standardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
    }
}
