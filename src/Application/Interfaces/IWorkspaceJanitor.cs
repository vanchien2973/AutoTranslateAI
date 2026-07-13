namespace Application.Interfaces;

public interface IWorkspaceJanitor
{
    IReadOnlyList<WorkspaceInfo> List();

    void Delete(string path);
}
