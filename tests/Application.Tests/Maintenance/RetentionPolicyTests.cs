namespace Application.Tests.Maintenance;

public class RetentionPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Given_Jobs_When_ExpiredJobs_Then_SelectsOnlyOlderThanRetention()
    {
        var jobs = new[]
        {
            new JobRetentionInfo(Guid.NewGuid(), Now.AddDays(-20)), // expired (>14d)
            new JobRetentionInfo(Guid.NewGuid(), Now.AddDays(-5)),  // kept
        };

        var expired = RetentionPolicy.ExpiredJobs(jobs, Now, retentionDays: 14);

        expired.Should().ContainSingle().Which.Should().Be(jobs[0].JobId);
    }

    [Fact]
    public void Given_TotalUnderCap_When_WorkspacesToPrune_Then_Empty()
    {
        var workspaces = new[]
        {
            new WorkspaceInfo(Guid.NewGuid(), "/ws/a", 100, Now),
            new WorkspaceInfo(Guid.NewGuid(), "/ws/b", 100, Now),
        };

        RetentionPolicy.WorkspacesToPrune(workspaces, maxBytes: 1000, protectedJobIds: new HashSet<Guid>())
            .Should().BeEmpty();
    }

    [Fact]
    public void Given_OverCap_When_WorkspacesToPrune_Then_DeletesOldestUntilUnderCap()
    {
        var old = new WorkspaceInfo(Guid.NewGuid(), "/ws/old", 600, Now.AddDays(-3));
        var recent = new WorkspaceInfo(Guid.NewGuid(), "/ws/recent", 600, Now);

        // total 1200 > 1000 cap → prune the oldest (600) leaves 600 ≤ 1000.
        var prune = RetentionPolicy.WorkspacesToPrune([recent, old], maxBytes: 1000, protectedJobIds: new HashSet<Guid>());

        prune.Should().ContainSingle().Which.Should().Be("/ws/old");
    }

    [Fact]
    public void Given_ProtectedActiveJob_When_WorkspacesToPrune_Then_SkipsIt()
    {
        var activeId = Guid.NewGuid();
        var active = new WorkspaceInfo(activeId, "/ws/active", 900, Now.AddDays(-10)); // oldest but protected
        var orphan = new WorkspaceInfo(Guid.NewGuid(), "/ws/orphan", 900, Now);

        var prune = RetentionPolicy.WorkspacesToPrune(
            [active, orphan], maxBytes: 1000, protectedJobIds: new HashSet<Guid> { activeId });

        prune.Should().ContainSingle().Which.Should().Be("/ws/orphan");
    }
}
