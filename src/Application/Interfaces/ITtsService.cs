using Application.Dtos;

namespace Application.Interfaces;

public interface ITtsService
{
    Task<TtsResult> SynthesizeAsync(TtsRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<VoiceInfo>> ListVoicesAsync(string languageCode, CancellationToken cancellationToken);
    bool SupportsLanguage(string languageCode);
    IReadOnlyCollection<string> SupportedLanguages { get; }
}
