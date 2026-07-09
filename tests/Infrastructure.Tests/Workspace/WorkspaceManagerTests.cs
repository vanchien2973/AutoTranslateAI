using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Workspace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Workspace;

public class WorkspaceManagerTests
{
    private static (IWorkspaceManager Manager, string Root) CreateManager()
    {
        var root = Path.Combine(Path.GetTempPath(), "ws-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new WorkspaceOptions { RootPath = root });
        var manager = new WorkspaceManager(options, Substitute.For<ILogger<WorkspaceManager>>());
        return (manager, root);
    }

    [Fact]
    public void Given_NewJob_When_GetOrCreateWorkspace_Then_CreatesDirectoryIdempotently()
    {
        // Arrange
        var (manager, root) = CreateManager();
        var jobId = Guid.NewGuid();

        try
        {
            // Act
            var first = manager.GetOrCreateWorkspace(jobId);
            var second = manager.GetOrCreateWorkspace(jobId); // second call must not throw

            // Assert
            first.Should().Be(second);
            Directory.Exists(first).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Given_NestedArtifact_When_GetArtifactPath_Then_CreatesParentDirectory()
    {
        // Arrange
        var (manager, root) = CreateManager();
        var jobId = Guid.NewGuid();
        manager.GetOrCreateWorkspace(jobId);

        try
        {
            // Act
            var path = manager.GetArtifactPath(jobId, "tts/seg-000.wav");

            // Assert
            Directory.Exists(Path.GetDirectoryName(path)).Should().BeTrue();
            path.Should().EndWith(Path.Combine("tts", "seg-000.wav"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Given_ExistingWorkspace_When_Cleanup_Then_DirectoryIsRemoved()
    {
        // Arrange
        var (manager, root) = CreateManager();
        var jobId = Guid.NewGuid();
        var jobRoot = manager.GetOrCreateWorkspace(jobId);
        File.WriteAllText(manager.GetArtifactPath(jobId, "audio.wav"), "data");

        try
        {
            // Act
            manager.Cleanup(jobId);

            // Assert
            Directory.Exists(jobRoot).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Given_MissingWorkspace_When_Cleanup_Then_DoesNotThrow()
    {
        // Arrange
        var (manager, _) = CreateManager();

        // Act
        var act = () => manager.Cleanup(Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }
}
