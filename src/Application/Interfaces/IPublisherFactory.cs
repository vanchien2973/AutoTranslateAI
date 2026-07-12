using Domain.Enums;

namespace Application.Interfaces;

public interface IPublisherFactory
{
    IPublisher Get(PublishPlatform platform);
}
