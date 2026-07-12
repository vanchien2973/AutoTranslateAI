using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

public sealed class PublisherFactory : IPublisherFactory
{
    private readonly IReadOnlyDictionary<PublishPlatform, IPublisher> _publishers;

    public PublisherFactory(IEnumerable<IPublisher> publishers) =>
        _publishers = publishers.ToDictionary(publisher => publisher.Platform);

    public IPublisher Get(PublishPlatform platform) =>
        _publishers.TryGetValue(platform, out var publisher)
            ? publisher
            : throw new NotSupportedException($"No publisher registered for {platform}.");
}
