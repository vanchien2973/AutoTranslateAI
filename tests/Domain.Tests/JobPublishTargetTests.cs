using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
namespace Domain.Tests;

public class JobPublishTargetTests
{
    private static DubbingJob NewJob() =>
        new(sourceUrl: "https://example.com/v", localFilePath: null, sourceLanguage: null,
            audioLanguage: "vi", subtitleLanguage: "vi", enableDubbing: true);

    [Fact]
    public void Given_NewJob_When_Created_Then_HasNoAutoPublishTargets()
    {
        NewJob().AutoPublishTargets.Should().BeEmpty();
    }

    [Fact]
    public void Given_Targets_When_Set_Then_Replaces_Previous()
    {
        var job = NewJob();

        job.SetAutoPublishTargets([new JobPublishTarget(job.Id, PublishPlatform.YouTube)]);
        job.SetAutoPublishTargets([new JobPublishTarget(job.Id, PublishPlatform.Facebook)]);

        job.AutoPublishTargets.Should().ContainSingle()
            .Which.Platform.Should().Be(PublishPlatform.Facebook);
    }

    [Fact]
    public void Given_EmptyList_When_Set_Then_AutoPublishIsOff()
    {
        var job = NewJob();
        job.SetAutoPublishTargets([new JobPublishTarget(job.Id, PublishPlatform.YouTube)]);

        job.SetAutoPublishTargets([]);

        job.AutoPublishTargets.Should().BeEmpty();
    }

    [Fact]
    public void Given_TitleOverLimit_When_Created_Then_Throws()
    {
        var tooLong = new string('a', JobPublishTarget.MaxTitleLength + 1);

        var act = () => new JobPublishTarget(Guid.NewGuid(), PublishPlatform.YouTube, null, tooLong);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_TitleAtLimit_When_Created_Then_Allowed()
    {
        var exact = new string('a', JobPublishTarget.MaxTitleLength);

        var target = new JobPublishTarget(Guid.NewGuid(), PublishPlatform.YouTube, null, exact);

        target.Title.Should().Be(exact);
    }

    [Fact]
    public void Given_NoConnectionId_When_Created_Then_LeavesChannelChoiceToPublishTime()
    {
        new JobPublishTarget(Guid.NewGuid(), PublishPlatform.YouTube).ConnectionId.Should().BeNull();
    }

    [Fact]
    public void Given_JobWithAutoPublish_When_Reopened_Then_TargetsCleared()
    {
        var job = NewJob();
        job.BeginPhase1Processing();
        job.SetAutoPublishTargets([new JobPublishTarget(job.Id, PublishPlatform.YouTube)]);
        job.MarkAwaitingReview();
        job.Confirm();
        job.BeginPhase2Processing();
        job.Complete("s3://out.mp4");

        // Reopening to re-render must not silently republish — platforms would create a duplicate video.
        job.ReopenForReview();

        job.AutoPublishTargets.Should().BeEmpty();
    }
}
