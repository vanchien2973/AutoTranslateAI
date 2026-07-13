namespace Application.Dtos;

public sealed record WorkspaceInfo(Guid? JobId, string Path, long SizeBytes, DateTimeOffset LastWriteUtc);

public sealed record JobRetentionInfo(Guid JobId, DateTimeOffset CompletedAt);
