using Infrastructure.Workspace;

namespace Infrastructure.Tests.Workspace;

public class WorkspacePathResolverTests
{
    [Fact]
    public void Given_RootAndJobId_When_ResolveJobRoot_Then_CombinesWithHexJobId()
    {
        // Arrange
        var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var jobRoot = WorkspacePathResolver.ResolveJobRoot("/app/workspace", jobId);

        // Assert
        jobRoot.Should().Be(Path.Combine("/app/workspace", "11111111111111111111111111111111"));
    }

    [Fact]
    public void Given_RelativeArtifact_When_ResolveArtifact_Then_PathIsInsideJobRoot()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobRoot = Path.GetFullPath(WorkspacePathResolver.ResolveJobRoot("/app/workspace", jobId));

        // Act
        var artifact = WorkspacePathResolver.ResolveArtifact("/app/workspace", jobId, "tts/seg-000.wav");

        // Assert
        artifact.Should().StartWith(jobRoot + Path.DirectorySeparatorChar);
        artifact.Should().EndWith(Path.Combine("tts", "seg-000.wav"));
    }

    [Fact]
    public void Given_TraversalPath_When_ResolveArtifact_Then_Throws()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act
        var act = () => WorkspacePathResolver.ResolveArtifact("/app/workspace", jobId, "../../etc/passwd");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_EmptyRelativePath_When_ResolveArtifact_Then_Throws(string relativePath)
    {
        // Act
        var act = () => WorkspacePathResolver.ResolveArtifact("/app/workspace", Guid.NewGuid(), relativePath);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
