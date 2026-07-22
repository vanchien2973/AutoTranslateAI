namespace Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string localPath, string key, CancellationToken cancellationToken);

    Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken cancellationToken);

    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
