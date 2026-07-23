namespace Application.Features.Admin.CleanupStorage;

public sealed record CleanupStorageResponse(
    bool DryRun,
    int LogosDeleted,
    int OutputsDeleted,
    IReadOnlyList<string> Keys);
