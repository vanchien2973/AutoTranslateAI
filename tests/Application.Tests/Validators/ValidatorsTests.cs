using Application.Features.Publishing.ConnectChannel;
using Application.Features.Publishing.GetChannelAuthUrl;
using Application.Features.Publishing.PublishJob;
using Application.Features.Publishing.SetPlatformCredential;
using Application.Features.Segments.AdjustSegmentTiming;
using Domain.Enums;

namespace Application.Tests.Validators;

public class ValidatorsTests
{
    private static readonly PublishTarget Target = new(PublishPlatform.YouTube, null, "Title", null, null);

    [Fact]
    public void SetPlatformCredential_Valid_And_Invalid()
    {
        var validator = new SetPlatformCredentialCommandValidator();

        validator.Validate(new SetPlatformCredentialCommand(PublishPlatform.YouTube, "cid", "secret", "https://app/cb"))
            .IsValid.Should().BeTrue();
        validator.Validate(new SetPlatformCredentialCommand(PublishPlatform.YouTube, "", "secret", null))
            .IsValid.Should().BeFalse();
        validator.Validate(new SetPlatformCredentialCommand(PublishPlatform.YouTube, "cid", "secret", "not-a-url"))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishJob_RequiresTargets()
    {
        var validator = new PublishJobCommandValidator();

        validator.Validate(new PublishJobCommand(Guid.NewGuid(), [Target])).IsValid.Should().BeTrue();
        validator.Validate(new PublishJobCommand(Guid.NewGuid(), [])).IsValid.Should().BeFalse();
        validator.Validate(new PublishJobCommand(Guid.Empty, [Target])).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConnectChannel_RequiresCodeAndRedirect()
    {
        var validator = new ConnectChannelCommandValidator();

        validator.Validate(new ConnectChannelCommand(PublishPlatform.YouTube, "code", "https://app/cb"))
            .IsValid.Should().BeTrue();
        validator.Validate(new ConnectChannelCommand(PublishPlatform.YouTube, "", "https://app/cb"))
            .IsValid.Should().BeFalse();
        validator.Validate(new ConnectChannelCommand(PublishPlatform.YouTube, "code", "bad"))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetChannelAuthUrl_RequiresAbsoluteRedirect()
    {
        var validator = new GetChannelAuthUrlValidator();

        validator.Validate(new GetChannelAuthUrlQuery(PublishPlatform.TikTok, "https://app/cb", null))
            .IsValid.Should().BeTrue();
        validator.Validate(new GetChannelAuthUrlQuery(PublishPlatform.TikTok, "relative/path", null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void AdjustSegmentTiming_EndMustExceedStart()
    {
        var validator = new AdjustSegmentTimingCommandValidator();

        validator.Validate(new AdjustSegmentTimingCommand(Guid.NewGuid(), Guid.NewGuid(), 1.0, 2.0))
            .IsValid.Should().BeTrue();
        validator.Validate(new AdjustSegmentTimingCommand(Guid.NewGuid(), Guid.NewGuid(), 2.0, 2.0))
            .IsValid.Should().BeFalse();
        validator.Validate(new AdjustSegmentTimingCommand(Guid.NewGuid(), Guid.NewGuid(), -1.0, 2.0))
            .IsValid.Should().BeFalse();
    }
}
