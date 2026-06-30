using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public sealed class R2StorageService : IStorageService, IDisposable
{
    private readonly R2Options _options;
    private readonly IAmazonS3 _s3;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(IOptions<R2Options> options, ILogger<R2StorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = _options.Region,
        };
        _s3 = new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
    }

    public async Task<string> UploadAsync(string localPath, string key, CancellationToken cancellationToken)
    {
        var transfer = new TransferUtility(_s3);
        await transfer.UploadAsync(localPath, _options.BucketName, key, cancellationToken);

        var url = R2UrlResolver.Resolve(_options.PublicUrl, key);
        _logger.LogInformation("Uploaded {Local} to R2 {Bucket}/{Key}", localPath, _options.BucketName, key);
        return url;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _s3.GetObjectAsync(_options.BucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _s3.DeleteObjectAsync(_options.BucketName, key, cancellationToken);

    public void Dispose() => _s3.Dispose();
}
