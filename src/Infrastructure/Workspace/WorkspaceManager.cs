using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Workspace;

public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly WorkspaceOptions _options;
    private readonly ILogger<WorkspaceManager> _logger;

    public WorkspaceManager(IOptions<WorkspaceOptions> options, ILogger<WorkspaceManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GetOrCreateWorkspace(Guid jobId)
    {
        var jobRoot = WorkspacePathResolver.ResolveJobRoot(_options.RootPath, jobId);
        Directory.CreateDirectory(jobRoot);
        return jobRoot;
    }

    public string GetArtifactPath(Guid jobId, string relativePath)
    {
        var path = WorkspacePathResolver.ResolveArtifact(_options.RootPath, jobId, relativePath);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return path;
    }

    public void Cleanup(Guid jobId)
    {
        var jobRoot = WorkspacePathResolver.ResolveJobRoot(_options.RootPath, jobId);
        if (!Directory.Exists(jobRoot))
        {
            return;
        }

        Directory.Delete(jobRoot, recursive: true);
        _logger.LogInformation("Cleaned up workspace for job {JobId}", jobId);
    }
}
