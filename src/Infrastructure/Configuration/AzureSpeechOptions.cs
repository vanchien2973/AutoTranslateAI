using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

public sealed class AzureSpeechOptions
{
    public const string SectionName = "Azure";

    [Required(AllowEmptyStrings = false)]
    public string SpeechKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SpeechRegion { get; init; } = "australiaeast";
    public string? Endpoint { get; init; }
}
