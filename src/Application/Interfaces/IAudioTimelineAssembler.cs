using Application.Dtos;

namespace Application.Interfaces;

public interface IAudioTimelineAssembler
{
    Task<string> AssembleAsync(TimelineAssemblyRequest request, CancellationToken cancellationToken);
}
