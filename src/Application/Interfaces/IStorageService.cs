namespace Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string localPath, string key, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
