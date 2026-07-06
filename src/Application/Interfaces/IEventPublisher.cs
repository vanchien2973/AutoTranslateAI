namespace Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken) where TEvent : class;
}
