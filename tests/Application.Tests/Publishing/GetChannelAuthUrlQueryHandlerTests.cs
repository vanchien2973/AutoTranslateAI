using Application.Features.Publishing.GetChannelAuthUrl;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Publishing;

public class GetChannelAuthUrlQueryHandlerTests
{
    [Fact]
    public async Task Given_NoCredentials_When_Handle_Then_CredentialsMissing()
    {
        var creds = Substitute.For<IPlatformCredentialRepository>();
        creds.GetAsync(PublishPlatform.YouTube, Arg.Any<CancellationToken>()).Returns((PlatformCredential?)null);
        var handler = new GetChannelAuthUrlQueryHandler(creds, Substitute.For<IOAuthProviderFactory>());

        var response = await handler.Handle(
            new GetChannelAuthUrlQuery(PublishPlatform.YouTube, "https://app/cb", null), CancellationToken.None);

        response.Status.Should().Be(AuthUrlStatus.CredentialsMissing);
    }

    [Fact]
    public async Task Given_Credentials_When_Handle_Then_ReturnsUrlAndEchoesState()
    {
        var creds = Substitute.For<IPlatformCredentialRepository>();
        creds.GetAsync(PublishPlatform.YouTube, Arg.Any<CancellationToken>())
            .Returns(new PlatformCredential(PublishPlatform.YouTube, "cid", "secret", null));
        var provider = Substitute.For<IOAuthProvider>();
        provider.BuildAuthorizationUrl(Arg.Any<OAuthAppCredentials>(), "https://app/cb", Arg.Any<string>())
            .Returns("https://consent");
        var factory = Substitute.For<IOAuthProviderFactory>();
        factory.Get(PublishPlatform.YouTube).Returns(provider);
        var handler = new GetChannelAuthUrlQueryHandler(creds, factory);

        var response = await handler.Handle(
            new GetChannelAuthUrlQuery(PublishPlatform.YouTube, "https://app/cb", "my-state"), CancellationToken.None);

        response.Status.Should().Be(AuthUrlStatus.Ok);
        response.Url.Should().Be("https://consent");
        response.State.Should().Be("my-state");
    }
}
