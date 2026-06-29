using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

/// <summary>
/// Cloudflare R2 (S3-compatible) for video output, accessed via the AWS SDK for .NET.
/// The S3 client uses <see cref="Endpoint"/> as ServiceURL, <see cref="Region"/> = "auto", and
/// path-style addressing (ForcePathStyle = true).
/// </summary>
public sealed class R2Options
{
    public const string SectionName = "R2";
    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKeyId { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretAccessKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string BucketName { get; init; } = "autotranslate-output";

    public string Region { get; init; } = "auto";
    public string? PublicUrl { get; init; }
}
