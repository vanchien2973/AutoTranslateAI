namespace Infrastructure.Configuration;

public sealed class PiperOptions
{
    public const string SectionName = "Piper";

    public string ExecutablePath { get; init; } = "/usr/local/bin/piper";

    public string ModelsPath { get; init; } = "/models/piper";
}
