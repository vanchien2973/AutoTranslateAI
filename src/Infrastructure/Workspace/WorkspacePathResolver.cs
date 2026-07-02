namespace Infrastructure.Workspace;

internal static class WorkspacePathResolver
{
    public static string ResolveJobRoot(string root, Guid jobId) =>
        Path.Combine(root, jobId.ToString("N")); // "N" = 32 hex digits, a safe directory name

    public static string ResolveArtifact(string root, Guid jobId, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        }

        var jobRoot = Path.GetFullPath(ResolveJobRoot(root, jobId));
        var combined = Path.GetFullPath(Path.Combine(jobRoot, relativePath));

        var isInsideJobRoot =
            string.Equals(combined, jobRoot, StringComparison.Ordinal) ||
            combined.StartsWith(jobRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);

        if (!isInsideJobRoot)
        {
            throw new InvalidOperationException(
                $"Artifact path '{relativePath}' escapes the job workspace.");
        }

        return combined;
    }
}
