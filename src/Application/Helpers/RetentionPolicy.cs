namespace Application.Helpers;

public static class RetentionPolicy
{
    public static IReadOnlyList<Guid> ExpiredJobs(
        IEnumerable<JobRetentionInfo> jobs,
        DateTimeOffset now,
        int retentionDays)
    {
        var cutoff = now.AddDays(-retentionDays);
        return jobs.Where(job => job.CompletedAt <= cutoff).Select(job => job.JobId).ToList();
    }

    public static IReadOnlyList<string> WorkspacesToPrune(
        IReadOnlyList<WorkspaceInfo> workspaces,
        long maxBytes,
        ISet<Guid> protectedJobIds)
    {
        var total = workspaces.Sum(workspace => workspace.SizeBytes);
        if (total <= maxBytes)
        {
            return [];
        }

        var prune = new List<string>();
        foreach (var workspace in workspaces.OrderBy(workspace => workspace.LastWriteUtc))
        {
            if (total <= maxBytes)
            {
                break;
            }

            if (workspace.JobId is { } jobId && protectedJobIds.Contains(jobId))
            {
                continue;
            }

            prune.Add(workspace.Path);
            total -= workspace.SizeBytes;
        }

        return prune;
    }
}
