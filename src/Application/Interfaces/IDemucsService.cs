using Application.Dtos;

namespace Application.Interfaces;

public interface IDemucsService
{
    Task<DemucsResult> SeparateAsync(DemucsRequest request, CancellationToken cancellationToken);
}
