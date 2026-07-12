using Domain.Entities;

namespace Application.Interfaces;

public interface IPublishStep
{
    Task<IReadOnlyList<PublishTargetResult>> ExecuteAsync(
        DubbingJob job,
        IReadOnlyList<PublishTarget> targets,
        CancellationToken cancellationToken);
}
