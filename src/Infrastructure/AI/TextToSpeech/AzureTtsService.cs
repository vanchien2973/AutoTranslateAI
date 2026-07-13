using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Resilience;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceInfo = Application.Dtos.VoiceInfo;

namespace Infrastructure.AI.TextToSpeech;

public sealed class AzureTtsService : ITtsService
{
    private readonly AzureSpeechOptions _options;
    private readonly ExternalApiResiliencePipeline _resilience;
    private readonly IUsageTracker _usage;
    private readonly ILogger<AzureTtsService> _logger;

    public AzureTtsService(
        IOptions<AzureSpeechOptions> options,
        ExternalApiResiliencePipeline resilience,
        IUsageTracker usage,
        ILogger<AzureTtsService> logger)
    {
        _options = options.Value;
        _resilience = resilience;
        _usage = usage;
        _logger = logger;
    }

    public async Task<TtsResult> SynthesizeAsync(TtsRequest request, CancellationToken cancellationToken)
    {
        var voice = string.IsNullOrWhiteSpace(request.VoiceId)
            ? AzureVoiceCatalog.ResolveVoice(request.LanguageCode, request.Gender)
            : request.VoiceId!;

        var ssml = SsmlBuilder.Build(request.Text, voice, request.LanguageCode, request.RateFactor);

        // Each retry attempt builds a fresh synthesizer so a spent/aborted one is never reused; the
        // per-attempt timeout in the pipeline guards against a hung Azure call.
        var durationMs = await _resilience.Pipeline.ExecuteAsync(
            async _ => await SynthesizeOnceAsync(ssml, request.OutputPath),
            cancellationToken);

        await _usage.RecordAsync(
            new UsageEntry("Azure", "Tts", Domain.Enums.UsageUnit.Characters, request.Text.Length),
            cancellationToken);

        _logger.LogInformation("Synthesized {Ms}ms with voice {Voice} to {Path}", durationMs, voice, request.OutputPath);
        return new TtsResult(request.OutputPath, durationMs, voice);
    }

    private async Task<long> SynthesizeOnceAsync(string ssml, string outputPath)
    {
        var speechConfig = CreateSpeechConfig();
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

        using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
        using var result = await synthesizer.SpeakSsmlAsync(ssml);

        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
        {
            var details = SpeechSynthesisCancellationDetails.FromResult(result);
            throw new InvalidOperationException(
                $"Azure TTS failed ({result.Reason}): {details.ErrorCode} - {details.ErrorDetails}");
        }

        return (long)result.AudioDuration.TotalMilliseconds;
    }

    public Task<IReadOnlyList<VoiceInfo>> ListVoicesAsync(string languageCode, CancellationToken cancellationToken) =>
        Task.FromResult(AzureVoiceCatalog.ListVoices(languageCode));

    public bool SupportsLanguage(string languageCode) => AzureVoiceCatalog.Supports(languageCode);

    public IReadOnlyCollection<string> SupportedLanguages => AzureVoiceCatalog.Languages;

    private SpeechConfig CreateSpeechConfig() =>
        string.IsNullOrWhiteSpace(_options.Endpoint)
            ? SpeechConfig.FromSubscription(_options.SpeechKey, _options.SpeechRegion)
            : SpeechConfig.FromEndpoint(new Uri(_options.Endpoint), _options.SpeechKey);
}
