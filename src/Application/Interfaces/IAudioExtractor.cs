using Application.Dtos;

namespace Application.Interfaces;

public interface IAudioExtractor
{
    Task<string> ExtractAudioAsync(AudioExtractionRequest request, CancellationToken cancellationToken);
}
