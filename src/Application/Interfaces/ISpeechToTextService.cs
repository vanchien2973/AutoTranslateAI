using Application.Dtos;

namespace Application.Interfaces;

public interface ISpeechToTextService
{
    Task<TranscriptionResult> TranscribeAsync(
        string audioPath,
        string? languageHint,
        CancellationToken cancellationToken);
}
