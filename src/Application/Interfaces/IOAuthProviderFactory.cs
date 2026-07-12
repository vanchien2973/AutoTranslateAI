using Domain.Enums;

namespace Application.Interfaces;

public interface IOAuthProviderFactory
{
    IOAuthProvider Get(PublishPlatform platform);
}
