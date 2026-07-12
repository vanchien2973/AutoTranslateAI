using Domain.Entities;

namespace Application.Interfaces;

public interface IPublishExecutor
{
    Task<IReadOnlyList<PublishTargetResult>> ExecuteAsync(
        DubbingJob job,
        IReadOnlyList<PublishTarget> targets,
        CancellationToken cancellationToken);
}
