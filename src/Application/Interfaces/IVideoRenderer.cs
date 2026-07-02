using Application.Dtos;

namespace Application.Interfaces;

public interface IVideoRenderer
{
    Task<string> RenderAsync(RenderRequest request, CancellationToken cancellationToken);
}
