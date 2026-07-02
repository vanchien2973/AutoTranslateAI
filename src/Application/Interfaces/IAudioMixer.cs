using Application.Dtos;

namespace Application.Interfaces;

public interface IAudioMixer
{
    Task<string> MixAsync(MixRequest request, CancellationToken cancellationToken);
}
