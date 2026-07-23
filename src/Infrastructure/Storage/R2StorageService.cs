using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
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
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        _s3 = new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
    }

    public async Task<string> UploadAsync(string localPath, string key, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            FilePath = localPath,
            UseChunkEncoding = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };
        await _s3.PutObjectAsync(request, cancellationToken);

        var url = R2UrlResolver.Resolve(_options.PublicUrl, key);
        _logger.LogInformation("Uploaded {Local} to R2 {Bucket}/{Key}", localPath, _options.BucketName, key);
        return url;
    }

    public async Task<string> UploadAsync(
        Stream content,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            UseChunkEncoding = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };
        await _s3.PutObjectAsync(request, cancellationToken);

        var url = R2UrlResolver.Resolve(_options.PublicUrl, key);
        _logger.LogInformation("Uploaded stream to R2 {Bucket}/{Key}", _options.BucketName, key);
        return url;
    }

    public async Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        };

        return await _s3.GetPreSignedURLAsync(request);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _s3.GetObjectAsync(_options.BucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _s3.DeleteObjectAsync(_options.BucketName, key, cancellationToken);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        string? continuationToken = null;
        do
        {
            var response = await _s3.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = _options.BucketName,
                    Prefix = string.IsNullOrEmpty(prefix) ? null : prefix,
                    ContinuationToken = continuationToken,
                },
                cancellationToken);

            keys.AddRange(response.S3Objects.Select(o => o.Key));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return keys;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_options.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public void Dispose() => _s3.Dispose();
}
