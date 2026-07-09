using Application.Features.Jobs.CreateJob;
using Application.Interfaces;

namespace Application.Tests.Jobs;

public class CreateJobCommandValidatorTests
{
    private static CreateJobCommandValidator ValidatorSupporting(params string[] supported)
    {
        var tts = Substitute.For<ITtsService>();
        tts.SupportsLanguage(Arg.Any<string>())
            .Returns(call => supported.Contains((string)call[0], StringComparer.OrdinalIgnoreCase));
        return new CreateJobCommandValidator(tts);
    }

    [Fact]
    public void Given_EmptySourceUrl_When_Validate_Then_IsInvalid()
    {
        ValidatorSupporting("vi")
            .Validate(new CreateJobCommand("", "vi", "vi", true, null, null, null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Given_SupportedAudioLanguageWithDubbing_When_Validate_Then_IsValid()
    {
        ValidatorSupporting("vi", "en")
            .Validate(new CreateJobCommand("https://youtu.be/x", "vi", "vi", true, null, null, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Given_UnsupportedAudioLanguageWithDubbing_When_Validate_Then_IsInvalid()
    {
        ValidatorSupporting("vi")
            .Validate(new CreateJobCommand("https://youtu.be/x", "xx", "vi", true, null, null, null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Given_UnsupportedAudioLanguageButDubbingOff_When_Validate_Then_IsValid()
    {
        // No dubbing -> no TTS needed, so the audio language is irrelevant.
        ValidatorSupporting("vi")
            .Validate(new CreateJobCommand("https://youtu.be/x", "xx", "vi", false, null, null, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Given_NullAudioLanguage_When_Validate_Then_IsValid()
    {
        // Handler defaults a null audio language to a supported one, so validation should not reject it.
        ValidatorSupporting("vi")
            .Validate(new CreateJobCommand("https://youtu.be/x", null, null, true, null, null, null)).IsValid.Should().BeTrue();
    }
}
