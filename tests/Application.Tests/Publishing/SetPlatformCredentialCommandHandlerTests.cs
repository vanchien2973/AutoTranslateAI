using Application.Features.Publishing.SetPlatformCredential;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Publishing;

public class SetPlatformCredentialCommandHandlerTests
{
    [Fact]
    public async Task Given_NoExistingCredential_When_Handle_Then_CreatesAndReturnsMaskedDto()
    {
        // Arrange
        var repo = Substitute.For<IPlatformCredentialRepository>();
        repo.GetAsync(PublishPlatform.YouTube, Arg.Any<CancellationToken>()).Returns((PlatformCredential?)null);
        var handler = new SetPlatformCredentialCommandHandler(repo);

        // Act
        var response = await handler.Handle(
            new SetPlatformCredentialCommand(PublishPlatform.YouTube, "cid", "secret", "https://app/cb"),
            CancellationToken.None);

        // Assert
        response.Credential.ClientId.Should().Be("cid");
        response.Credential.HasSecret.Should().BeTrue();
        await repo.Received(1).UpsertAsync(
            Arg.Is<PlatformCredential>(c => c.Platform == PublishPlatform.YouTube && c.ClientSecret == "secret"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ExistingCredential_When_Handle_Then_UpdatesInPlace()
    {
        // Arrange
        var existing = new PlatformCredential(PublishPlatform.TikTok, "old", "old-secret", null);
        var repo = Substitute.For<IPlatformCredentialRepository>();
        repo.GetAsync(PublishPlatform.TikTok, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new SetPlatformCredentialCommandHandler(repo);

        // Act
        await handler.Handle(
            new SetPlatformCredentialCommand(PublishPlatform.TikTok, "new", "new-secret", null),
            CancellationToken.None);

        // Assert
        existing.ClientId.Should().Be("new");
        existing.ClientSecret.Should().Be("new-secret");
        existing.UpdatedAt.Should().NotBeNull();
    }
}
