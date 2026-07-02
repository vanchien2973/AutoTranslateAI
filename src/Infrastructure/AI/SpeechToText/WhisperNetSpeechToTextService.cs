using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whisper.net;

namespace Infrastructure.AI.SpeechToText;

public sealed class WhisperNetSpeechToTextService : ISpeechToTextService, IDisposable
{
    private readonly WhisperNetOptions _options;
    private readonly ILogger<WhisperNetSpeechToTextService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Lazy<Task<WhisperFactory>> _factory;

    public WhisperNetSpeechToTextService(
        IOptions<WhisperNetOptions> options,
        ILogger<WhisperNetSpeechToTextService> logger)
    {
        _options = options.Value;
        _logger = logger;
        // One-time, possibly large download — no per-request timeout (cancellation still works).
        _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _factory = new Lazy<Task<WhisperFactory>>(InitializeFactoryAsync);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string audioPath,
        string? languageHint,
        CancellationToken cancellationToken)
    {
        var factory = await _factory.Value;
        var language = string.IsNullOrWhiteSpace(languageHint) ? "auto" : languageHint;

        await using var processor = factory.CreateBuilder()
            .WithLanguage(language)
            .Build();

        await using var audioStream = File.OpenRead(audioPath);

        var segments = new List<TranscriptSegment>();
        var detectedLanguage = language;
        var index = 0;

        await foreach (var segment in processor.ProcessAsync(audioStream, cancellationToken))
        {
            segments.Add(new TranscriptSegment(
                index++,
                segment.Start.TotalSeconds,
                segment.End.TotalSeconds,
                segment.Text.Trim()));

            if (!string.IsNullOrEmpty(segment.Language))
            {
                detectedLanguage = segment.Language;
            }
        }

        _logger.LogInformation(
            "Transcribed {Count} segments from {Audio} (language {Language})",
            segments.Count, audioPath, detectedLanguage);

        return new TranscriptionResult(segments, detectedLanguage);
    }

    private async Task<WhisperFactory> InitializeFactoryAsync()
    {
        await EnsureModelDownloadedAsync(CancellationToken.None);
        return WhisperFactory.FromPath(_options.ModelPath);
    }

    private async Task EnsureModelDownloadedAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_options.ModelPath))
        {
            return;
        }

        var url = WhisperModelUrlResolver.BuildDownloadUrl(_options.Model);
        var directory = Path.GetDirectoryName(_options.ModelPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger.LogInformation(
            "Whisper model not found at {Path}; downloading '{Model}' from {Url}",
            _options.ModelPath, _options.Model, url);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Stream to a temp file then move, so an interrupted download is never mistaken for a valid model.
        var tempPath = _options.ModelPath + ".part";
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destination = File.Create(tempPath))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        File.Move(tempPath, _options.ModelPath, overwrite: true);
        _logger.LogInformation("Whisper model saved to {Path}", _options.ModelPath);
    }

    public void Dispose()
    {
        _httpClient.Dispose();

        if (_factory.IsValueCreated && _factory.Value.IsCompletedSuccessfully)
        {
            _factory.Value.Result.Dispose();
        }
    }
}
