using Application.Interfaces;
using MassTransit;

namespace Infrastructure.Messaging;

public sealed class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publish;

    public MassTransitEventPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : class =>
        _publish.Publish(message, cancellationToken);
}
