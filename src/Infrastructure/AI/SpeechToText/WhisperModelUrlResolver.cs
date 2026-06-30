namespace Infrastructure.AI.SpeechToText;

internal static class WhisperModelUrlResolver
{
    private const string BaseUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main";

    private static readonly HashSet<string> KnownModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "tiny", "tiny.en",
        "base", "base.en",
        "small", "small.en",
        "medium", "medium.en",
        "large-v1", "large-v2", "large-v3", "large-v3-turbo",
    };

    public static string BuildDownloadUrl(string model)
    {
        if (string.IsNullOrWhiteSpace(model) || !KnownModels.Contains(model))
        {
            throw new ArgumentException(
                $"Unknown whisper model '{model}'. Known models: {string.Join(", ", KnownModels)}",
                nameof(model));
        }

        return $"{BaseUrl}/ggml-{model.ToLowerInvariant()}.bin";
    }
}
