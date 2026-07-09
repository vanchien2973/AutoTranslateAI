using Application.Features.Jobs.CreateJob;
using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using Domain.Enums;
using Shared.Enums;

namespace Application.Tests.Jobs;

public class CreateJobCommandHandlerTests
{
    [Fact]
    public async Task Given_MissingOptionals_When_Handle_Then_AppliesDefaultsAndPublishesRequested()
    {
        // Arrange
        DubbingJob? saved = null;
        var jobs = Substitute.For<IDubbingJobRepository>();
        await jobs.AddAsync(Arg.Do<DubbingJob>(job => saved = job), Arg.Any<CancellationToken>());
        var events = Substitute.For<IEventPublisher>();
        var handler = new CreateJobCommandHandler(jobs, events);

        // Act
        var response = await handler.Handle(new CreateJobCommand("https://youtu.be/x", null, null, null, null, null, null), CancellationToken.None);

        // Assert
        saved!.AudioLanguage.Should().Be("vi");
        saved.SubtitleLanguage.Should().Be("vi");
        saved.EnableDubbing.Should().BeTrue();
        saved.VoiceGender.Should().Be(VoiceGender.Female);
        saved.SubtitleMode.Should().Be(SubtitleMode.Softsub);
        saved.BgmMode.Should().Be(BgmMode.DemucsAI);
        response.JobId.Should().Be(saved.Id);
        await events.Received(1).PublishAsync(
            Arg.Is<DubbingJobRequested>(message => message.JobId == saved.Id && message.AudioLanguage == "vi"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ExplicitValues_When_Handle_Then_UsesThem()
    {
        // Arrange
        DubbingJob? saved = null;
        var jobs = Substitute.For<IDubbingJobRepository>();
        await jobs.AddAsync(Arg.Do<DubbingJob>(job => saved = job), Arg.Any<CancellationToken>());
        var handler = new CreateJobCommandHandler(jobs, Substitute.For<IEventPublisher>());

        // Act
        await handler.Handle(new CreateJobCommand("https://youtu.be/x", "en", "fr", false, VoiceGender.Male, SubtitleMode.Hardsub, BgmMode.Duck), CancellationToken.None);

        // Assert
        saved!.AudioLanguage.Should().Be("en");
        saved.SubtitleLanguage.Should().Be("fr");
        saved.EnableDubbing.Should().BeFalse();
        saved.VoiceGender.Should().Be(VoiceGender.Male);
        saved.SubtitleMode.Should().Be(SubtitleMode.Hardsub);
        saved.BgmMode.Should().Be(BgmMode.Duck);
    }
}
