using Domain.Enums;

namespace Application.Interfaces;

public interface IPublisher
{
    PublishPlatform Platform { get; }

    Task<PublishOutcome> PublishAsync(PublishRequest request, CancellationToken cancellationToken);
}
