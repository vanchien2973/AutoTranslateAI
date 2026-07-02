namespace Infrastructure.Media.Downloader;

internal static class YtDlpArguments
{
    public static IReadOnlyList<string> BuildDownload(string url, string outputDirectory)
    {
        var outputTemplate = Path.Combine(outputDirectory, "source.%(ext)s");
        return
        [
            url,
            "-o", outputTemplate,
            "--no-playlist",
            "--no-progress",
            "--no-simulate",
            "--print", "after_move:filepath",
        ];
    }
}
