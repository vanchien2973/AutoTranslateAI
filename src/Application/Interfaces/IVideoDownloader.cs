using Application.Dtos;

namespace Application.Interfaces;

public interface IVideoDownloader
{
    Task<DownloadResult> DownloadAsync(DownloadRequest request, CancellationToken cancellationToken);
}
