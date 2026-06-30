using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

public sealed class WhisperNetOptions
{
    public const string SectionName = "WhisperNet";
    [Required(AllowEmptyStrings = false)]
    public string ModelPath { get; init; } = string.Empty;
    public string Model { get; init; } = "base";
}
