using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Workspace;

public sealed class WorkspaceJanitor : IWorkspaceJanitor
{
    private readonly string _root;
    private readonly ILogger<WorkspaceJanitor> _logger;

    public WorkspaceJanitor(IOptions<WorkspaceOptions> options, ILogger<WorkspaceJanitor> logger)
    {
        _root = options.Value.RootPath;
        _logger = logger;
    }

    public IReadOnlyList<WorkspaceInfo> List()
    {
        if (!Directory.Exists(_root))
        {
            return [];
        }

        var infos = new List<WorkspaceInfo>();
        foreach (var dir in Directory.EnumerateDirectories(_root))
        {
            var name = Path.GetFileName(dir);
            Guid? jobId = Guid.TryParseExact(name, "N", out var parsed) ? parsed : null;

            long size = 0;
            var lastWrite = new DateTimeOffset(Directory.GetLastWriteTimeUtc(dir), TimeSpan.Zero);
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var info = new FileInfo(file);
                    size += info.Length;
                    var written = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
                    if (written > lastWrite)
                    {
                        lastWrite = written;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to size workspace {Dir}", dir);
            }

            infos.Add(new WorkspaceInfo(jobId, dir, size, lastWrite));
        }

        return infos;
    }

    public void Delete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                _logger.LogInformation("Pruned workspace {Path}", path);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to prune workspace {Path}", path);
        }
    }
}
