using Application.Interfaces;

namespace Infrastructure.Publishing;

internal sealed class TempMediaFile : IAsyncDisposable
{
    private TempMediaFile(string path) => Path = path;

    public string Path { get; }

    public long Length => new FileInfo(Path).Length;

    public static async Task<TempMediaFile> DownloadAsync(IStorageService storage, string storageKey, CancellationToken cancellationToken)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ata-publish-{Guid.NewGuid():N}.mp4");
        await using var source = await storage.DownloadAsync(storageKey, cancellationToken);
        await using var file = File.Create(path);
        await source.CopyToAsync(file, cancellationToken);
        return new TempMediaFile(path);
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
        catch
        {
            // Clean up best-effort — do not throw errors when disposing.
        }

        return ValueTask.CompletedTask;
    }
}
