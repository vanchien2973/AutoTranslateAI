namespace Application.Interfaces;

public interface IWorkspaceManager
{
    string GetOrCreateWorkspace(Guid jobId);
    string GetArtifactPath(Guid jobId, string relativePath);
    void Cleanup(Guid jobId);
}
