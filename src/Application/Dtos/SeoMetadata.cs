namespace Application.Dtos;

public sealed record SeoMetadata(string Title, string Description, IReadOnlyList<string> Tags);
