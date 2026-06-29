using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gpt-4.1-nano";

    public bool UseBatchApi { get; init; } = true;
}
