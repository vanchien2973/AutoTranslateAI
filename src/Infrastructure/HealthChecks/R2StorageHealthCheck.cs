using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Infrastructure.HealthChecks;

/// <summary>
/// Readiness check for Cloudflare R2: builds an S3 client (path-style, region "auto") against the
/// configured endpoint + credentials and lists one object from the bucket. Verifies endpoint,
/// credentials, and bucket access in one call. Options are read lazily so missing credentials
/// surface as Unhealthy rather than throwing at construction.
/// </summary>
public sealed class R2StorageHealthCheck : IHealthCheck
{
    private readonly IOptions<R2Options> _options;

    public R2StorageHealthCheck(IOptions<R2Options> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var o = _options.Value; // triggers [Required] validation; caught below if unset

            var config = new AmazonS3Config
            {
                ServiceURL = o.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = o.Region,
            };
            using var client = new AmazonS3Client(new BasicAWSCredentials(o.AccessKeyId, o.SecretAccessKey), config);

            await client.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = o.BucketName, MaxKeys = 1 },
                cancellationToken);

            return HealthCheckResult.Healthy($"R2 bucket '{o.BucketName}' reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("R2 not reachable / bucket inaccessible", ex);
        }
    }
}
