using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceInfo = Application.Dtos.VoiceInfo;

namespace Infrastructure.AI.TextToSpeech;

public sealed class AzureTtsService : ITtsService
{
    private readonly AzureSpeechOptions _options;
    private readonly ILogger<AzureTtsService> _logger;

    public AzureTtsService(IOptions<AzureSpeechOptions> options, ILogger<AzureTtsService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TtsResult> SynthesizeAsync(TtsRequest request, CancellationToken cancellationToken)
    {
        var voice = string.IsNullOrWhiteSpace(request.VoiceId)
            ? AzureVoiceCatalog.ResolveVoice(request.LanguageCode, request.Gender)
            : request.VoiceId!;

        var speechConfig = CreateSpeechConfig();
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

        using var audioConfig = AudioConfig.FromWavFileOutput(request.OutputPath);
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

        var ssml = SsmlBuilder.Build(request.Text, voice, request.LanguageCode, request.RateFactor);
        using var result = await synthesizer.SpeakSsmlAsync(ssml);

        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
        {
            var details = SpeechSynthesisCancellationDetails.FromResult(result);
            throw new InvalidOperationException(
                $"Azure TTS failed ({result.Reason}): {details.ErrorCode} - {details.ErrorDetails}");
        }

        var durationMs = (long)result.AudioDuration.TotalMilliseconds;
        _logger.LogInformation("Synthesized {Ms}ms with voice {Voice} to {Path}", durationMs, voice, request.OutputPath);

        return new TtsResult(request.OutputPath, durationMs, voice);
    }

    public Task<IReadOnlyList<VoiceInfo>> ListVoicesAsync(string languageCode, CancellationToken cancellationToken) =>
        Task.FromResult(AzureVoiceCatalog.ListVoices(languageCode));

    private SpeechConfig CreateSpeechConfig() =>
        string.IsNullOrWhiteSpace(_options.Endpoint)
            ? SpeechConfig.FromSubscription(_options.SpeechKey, _options.SpeechRegion)
            : SpeechConfig.FromEndpoint(new Uri(_options.Endpoint), _options.SpeechKey);
}
