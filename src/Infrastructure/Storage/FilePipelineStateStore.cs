using System.Text.Json;
using Application.Interfaces;
using Application.Pipeline;

namespace Infrastructure.Storage;

public sealed class FilePipelineStateStore : IPipelineStateStore
{
    private const string StateFileName = "pipeline-state.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly IWorkspaceManager _workspace;

    public FilePipelineStateStore(IWorkspaceManager workspace) => _workspace = workspace;

    public async Task<PipelineStateSnapshot?> LoadAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var path = _workspace.GetArtifactPath(jobId, StateFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<PipelineStateSnapshot>(stream, SerializerOptions, cancellationToken);
    }

    public async Task SaveAsync(Guid jobId, PipelineStateSnapshot snapshot, CancellationToken cancellationToken)
    {
        var path = _workspace.GetArtifactPath(jobId, StateFileName);

        // Write to a temp file then move, so a crash mid-write never leaves a half-written snapshot.
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
