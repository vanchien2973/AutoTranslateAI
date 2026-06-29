namespace Infrastructure.Configuration;

public sealed class ProviderOptions
{
    public const string SectionName = "Providers";

    public string Tts { get; init; } = "Azure";            // Azure | Piper
    public string SpeechToText { get; init; } = "WhisperNet";
    public string Translation { get; init; } = "OpenAI";   // OpenAI | Ollama
    public string Storage { get; init; } = "R2";           // R2 | Local | AzureBlob
}
