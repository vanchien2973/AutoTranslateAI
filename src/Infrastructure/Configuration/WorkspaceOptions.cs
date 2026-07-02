namespace Infrastructure.Configuration;

public sealed class WorkspaceOptions
{
    public const string SectionName = "Workspace";

    public string RootPath { get; init; } = "/app/workspace";
}
